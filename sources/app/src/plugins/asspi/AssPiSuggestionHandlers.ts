/*
 * AssPI fake-api handlers for time entry suggestions.
 *
 * Fulfils the RESTful `timeEntrySuggestions` + `issueDetails` endpoints entirely
 * client-side: pluggable providers (Jira today) yield candidates, projects and
 * activities are resolved via the existing legacy handlers, times are laid out in
 * the day's free slots, and dismissals/positions are persisted in IndexedDB.
 *
 * All of this is throwaway. When a real backend exists it produces the same DTOs
 * server-side and this whole plugins/asspi folder is deleted.
 */

import { formatISO } from "date-fns";
import client, { AssPiHttpError } from "@/plugins/asspi/AssPiClient";
import { findProjectIdByTaskId, loadActivities, loadProjects, loadTimeEntries } from "@/plugins/asspi/AssPiHandlers";
import { fmtDate, HtmlParser, parseGermanDate } from "@/plugins/asspi/HtmlParser";
import { getIssueDetails as jiraIssueDetails, type IssueDetails } from "@/plugins/asspi/jira/JiraClient";
import { layoutFreeSlots, type BusyInterval } from "@/plugins/asspi/suggestions/freeSlotLayout";
import { suggestionLog } from "@/plugins/asspi/suggestions/log";
import { jiraProvider } from "@/plugins/asspi/suggestions/providers/JiraProvider";
import { SuggestionDb } from "@/plugins/asspi/suggestions/SuggestionDb";
import { idFromSourceKey, sourceKeyOf, type SuggestionCandidate, type SuggestionProvider } from "@/plugins/asspi/suggestions/types";

// The wire shape GET/PUT return; matches what a real backend would send. Dates
// are real Date instances (in-process) — the service tolerates string|Date.
export type TimeEntrySuggestionDTO = {
    id: number;
    taskId: string;
    dateStarted: Date;
    dateEnded: Date;
    project: { id: number; name: string } | null;
    activity: { id: number; name: string } | null;
    comment: string | null;
};

// Body of PUT timeEntrySuggestions/{id} (dates arrive as ISO strings after JSON).
export type TimeEntrySuggestionUpdateDTO = {
    taskId: string;
    dateStarted: string;
    dateEnded: string;
    projectId: number | null;
    activityId: number | null;
    comment: string | null;
};

// Registered suggestion sources. Add Outlook/Teams providers here later.
const providers: SuggestionProvider[] = [jiraProvider];

const DEFAULT_ACTIVITY_NAME = "SWE";
const RESOLVE_CONCURRENCY = 6;

// id → metadata for the currently-loaded suggestions, so PUT/DELETE/accept (which
// only carry the numeric id) can persist against the stable key. The task id and
// day are kept too, so an accepted suggestion can later be reconciled against the
// real entries (see recommendAgain). Rebuilt on every load.
type SuggestionMeta = { sourceKey: string; taskId: string; date: string };
const idToMeta = new Map<number, SuggestionMeta>();

// Run `worker` over `items` with a fixed concurrency cap, preserving order.
async function mapLimited<T, R>(items: T[], limit: number, worker: (item: T) => Promise<R>): Promise<R[]> {
    const results: R[] = new Array(items.length);
    let next = 0;
    const runners = Array.from({ length: Math.min(limit, items.length) }, async () => {
        while (next < items.length) {
            const i = next++;
            results[i] = await worker(items[i]);
        }
    });
    await Promise.all(runners);
    return results;
}

const isoDay = (d: Date): string => formatISO(d, { representation: "date" });
const minutesOfDay = (d: Date): number => d.getHours() * 60 + d.getMinutes();

// Light existing-entry fetch for layout: the list page already carries start/end
// (and the task key), so we skip the per-entry detail forms loadTimeEntries does.
async function fetchExistingRows(from: Date, to: Date): Promise<{ start: Date; end: Date; taskId: string }[]> {
    const q = `/?from=${encodeURIComponent(fmtDate(from))}&to=${encodeURIComponent(fmtDate(to))}&searchterm=&filter=`;
    const r = await client.getPage(q);
    if (!r.ok) return [];
    return HtmlParser.parseEntryList(r.text).map((row) => ({ start: row.start, end: row.end, taskId: row.taskId }));
}

// Normalized `day␟taskId` key, so an accepted suggestion (now a real entry)
// is not resurfaced. Task keys are compared case-insensitively.
const acceptedKey = (isoDayStr: string, taskId: string): string => `${isoDayStr}␟${taskId.trim().toUpperCase()}`;

type Resolved = {
    candidate: SuggestionCandidate;
    project: { id: number; name: string } | null;
    activity: { id: number; name: string } | null;
};

// Resolve a candidate's project (via the task→project mapping) and default
// activity. Project/activity are left null when they cannot be resolved — the
// suggestion is still shown and the user assigns them on accept.
async function resolveProjectAndActivity(candidate: SuggestionCandidate, projectNames: Map<number, string>): Promise<Resolved> {
    const projectId = await findProjectIdByTaskId(candidate.taskId);
    if (projectId == null) {
        suggestionLog.info(`"${candidate.taskId}" (${candidate.date}): no project mapped in the legacy timetracking site — suggesting without project/activity`);
        return { candidate, project: null, activity: null };
    }

    const project = { id: Number(projectId), name: projectNames.get(Number(projectId)) ?? "" };
    const activities = await loadActivities(String(projectId));
    if (!activities.length) {
        suggestionLog.info(`"${candidate.taskId}" (${candidate.date}): project ${projectId} has no activities — suggesting without activity`);
        return { candidate, project, activity: null };
    }

    const preferred = activities.find((a) => (a.name || "").trim().toLowerCase() === DEFAULT_ACTIVITY_NAME.toLowerCase());
    const activity = preferred ?? activities[0];
    return { candidate, project, activity: { id: Number(activity.id), name: activity.name } };
}

/* ── GET timeEntrySuggestions?from&to ── */

export async function loadSuggestions(fromIso: string, toIso: string): Promise<TimeEntrySuggestionDTO[]> {
    const from = new Date(`${fromIso}T00:00:00`);
    const to = new Date(`${toIso}T23:59:59`);
    const range = { from, to };
    suggestionLog.info(`loading for ${fromIso}..${toIso}`);

    // 1. Gather candidates from every provider, plus dismissals, accepted keys
    //    and the day's existing entries (the latter drive both the free-slot
    //    layout and a secondary "already booked" filter below).
    const [rawCandidates, dismissed, acceptedKeys, existingRows] = await Promise.all([
        Promise.all(
            providers.map((p) =>
                p.getCandidates(range).catch((error) => {
                    suggestionLog.error(`provider "${p.id}" failed:`, error);
                    return [];
                })
            )
        ).then((lists) => lists.flat()),
        SuggestionDb.getDismissedKeys(),
        SuggestionDb.getAcceptedKeys(),
        fetchExistingRows(from, to).catch((error) => {
            // Non-fatal: suggestions still render, just without avoiding existing
            // entries for layout or filtering out already-booked ones.
            suggestionLog.warn("could not load existing entries:", error);
            return [];
        })
    ]);
    suggestionLog.info(
        `${rawCandidates.length} candidate(s) from providers, ${dismissed.size} dismissed / ${acceptedKeys.size} accepted key(s) stored, ${existingRows.length} existing entry/entries`
    );

    // Task keys already booked on a given day → a matching suggestion is already
    // covered by a real entry. A best-effort backstop to the persisted accepted
    // keys, in case an entry was created outside the suggestion flow.
    const bookedByEntry = new Set<string>();
    for (const row of existingRows) {
        if (row.taskId) bookedByEntry.add(acceptedKey(isoDay(row.start), row.taskId));
    }

    // Drop candidates that were dismissed, already accepted (persisted — survives
    // "recommend again"), or already have a matching entry on that day.
    const candidates = rawCandidates.filter(
        (c) => !dismissed.has(sourceKeyOf(c)) && !acceptedKeys.has(sourceKeyOf(c)) && !bookedByEntry.has(acceptedKey(c.date, c.taskId))
    );
    if (!candidates.length) {
        suggestionLog.info(rawCandidates.length ? "all candidates were dismissed, accepted or already booked — nothing to show" : "no candidates produced (Jira returned nothing for this range, or the bridge failed)");
        idToMeta.clear();
        return [];
    }

    // 2. Resolve project + activity where possible (kept even when unresolved).
    const projectNames = new Map((await loadProjects()).map((p) => [Number(p.id), p.name]));
    const resolvedList = (
        await mapLimited(candidates, RESOLVE_CONCURRENCY, (c) =>
            resolveProjectAndActivity(c, projectNames).catch((error) => {
                suggestionLog.error(`resolving "${c.taskId}" (${c.date}) failed:`, error);
                return null;
            })
        )
    ).filter((r): r is Resolved => r !== null);
    const withProject = resolvedList.filter((r) => r.project !== null).length;
    suggestionLog.info(`${resolvedList.length} candidate(s) kept (${withProject} with a project, ${resolvedList.length - withProject} without)`);

    // 3. Existing entries per day feed the free-slot layout.
    const busyByDay = new Map<string, BusyInterval[]>();
    for (const row of existingRows) {
        const iso = isoDay(row.start);
        const list = busyByDay.get(iso) ?? [];
        list.push({ start: minutesOfDay(row.start), end: minutesOfDay(row.end) });
        busyByDay.set(iso, list);
    }

    // 4. Group by day, lay out times, then build DTOs (applying persisted positions).
    const positions = await SuggestionDb.getPositions();
    const byDay = new Map<string, Resolved[]>();
    for (const r of resolvedList) {
        const list = byDay.get(r.candidate.date) ?? [];
        list.push(r);
        byDay.set(r.candidate.date, list);
    }

    idToMeta.clear();
    const result: TimeEntrySuggestionDTO[] = [];
    for (const [iso, dayResolved] of byDay) {
        const midnight = new Date(`${iso}T00:00:00`);

        // A suggestion has a fixed time when the user has moved it (persisted) or
        // the provider gave a concrete time (e.g. a meeting). Everything else is
        // auto-placed. The fixed ones are treated as busy so auto-placed slots
        // never overlap them — one suggestion per timeslot.
        const fixedTimes = new Map<string, { start: Date; end: Date }>();
        const autoCandidates: Resolved[] = [];
        for (const r of dayResolved) {
            const sourceKey = sourceKeyOf(r.candidate);
            const persisted = positions.get(sourceKey);
            if (persisted) {
                fixedTimes.set(sourceKey, { start: new Date(persisted.start), end: new Date(persisted.end) });
            } else if (r.candidate.preferredStart && r.candidate.preferredEnd) {
                fixedTimes.set(sourceKey, { start: r.candidate.preferredStart, end: r.candidate.preferredEnd });
            } else {
                autoCandidates.push(r);
            }
        }

        const busy = [...(busyByDay.get(iso) ?? [])];
        for (const { start, end } of fixedTimes.values()) {
            busy.push({ start: minutesOfDay(start), end: minutesOfDay(end) });
        }
        const slots = layoutFreeSlots(autoCandidates.length, busy);

        let slotIdx = 0;
        for (const r of dayResolved) {
            const sourceKey = sourceKeyOf(r.candidate);
            const id = idFromSourceKey(sourceKey);
            idToMeta.set(id, { sourceKey, taskId: r.candidate.taskId, date: r.candidate.date });

            let start: Date;
            let end: Date;
            const fixed = fixedTimes.get(sourceKey);
            if (fixed) {
                start = fixed.start;
                end = fixed.end;
            } else {
                const slot = slots[slotIdx++];
                start = new Date(midnight);
                start.setHours(Math.floor(slot.start / 60), slot.start % 60, 0, 0);
                end = new Date(midnight);
                end.setHours(Math.floor(slot.end / 60), slot.end % 60, 0, 0);
            }

            result.push({
                id,
                taskId: r.candidate.taskId,
                dateStarted: start,
                dateEnded: end,
                project: r.project,
                activity: r.activity,
                comment: null
            });
        }
    }

    suggestionLog.info(`returning ${result.length} suggestion(s)`);
    return result;
}

/* ── PUT timeEntrySuggestions/{id} ── */

export async function updateSuggestion(id: number, update: TimeEntrySuggestionUpdateDTO): Promise<TimeEntrySuggestionDTO> {
    const sourceKey = idToMeta.get(id)?.sourceKey;
    const start = new Date(update.dateStarted);
    const end = new Date(update.dateEnded);
    if (sourceKey) await SuggestionDb.savePosition(sourceKey, { start: start.getTime(), end: end.getTime() });

    const projectNames = new Map((await loadProjects()).map((p) => [Number(p.id), p.name]));
    const activities = update.projectId != null ? await loadActivities(String(update.projectId)).catch(() => []) : [];

    return {
        id,
        taskId: update.taskId,
        dateStarted: start,
        dateEnded: end,
        project: update.projectId != null ? { id: update.projectId, name: projectNames.get(update.projectId) ?? "" } : null,
        activity: update.activityId != null ? { id: update.activityId, name: activities.find((a) => Number(a.id) === update.activityId)?.name ?? "" } : null,
        comment: update.comment
    };
}

/* ── DELETE timeEntrySuggestions/{id} ── */

export async function dismissSuggestion(id: number): Promise<void> {
    const sourceKey = idToMeta.get(id)?.sourceKey;
    if (sourceKey) await SuggestionDb.dismiss(sourceKey);
}

/* ── POST timeEntrySuggestions/{id}/accept ── */

// The suggestion was turned into a real time entry. Record it (with its task id
// and day) so it is not suggested again while that entry exists — including
// across "recommend again", unlike a plain dismiss. If the entry is later
// deleted, recommendAgain reconciles it away so it can resurface.
export async function acceptSuggestion(id: number): Promise<void> {
    const meta = idToMeta.get(id);
    if (meta) await SuggestionDb.accept(meta.sourceKey, meta.taskId, meta.date);
}

/* ── POST timeEntrySuggestions/recommendAgain ── */

export async function recommendAgain(): Promise<void> {
    await SuggestionDb.clearDismissals();

    // Reconcile accepted suggestions against reality: an accepted suggestion whose
    // time entry was since deleted should be suggested again. Accepted records are
    // otherwise permanent, so this "force recommend" is the moment we verify them.
    const records = (await SuggestionDb.getAcceptedRecords()).filter((r) => r.taskId && r.date);
    if (!records.length) return;

    const isoDates = records.map((r) => r.date).sort();
    const from = new Date(`${isoDates[0]}T00:00:00`);
    const to = new Date(`${isoDates[isoDates.length - 1]}T23:59:59`);

    let entries;
    try {
        // loadTimeEntries reads each entry's form (reliable task id), unlike the
        // list-row scrape used for layout.
        entries = await loadTimeEntries(fmtDate(from), fmtDate(to));
    } catch (error) {
        // Can't verify → keep accepted suppressed rather than risk duplicates.
        suggestionLog.warn("recommendAgain: could not load entries to reconcile accepted suggestions; leaving them suppressed:", error);
        return;
    }

    const booked = new Set<string>();
    for (const entry of entries) {
        const day = parseGermanDate(entry.startDate);
        if (day && entry.taskId) booked.add(acceptedKey(isoDay(day), entry.taskId));
    }

    let resurfaced = 0;
    for (const record of records) {
        if (!booked.has(acceptedKey(record.date, record.taskId))) {
            await SuggestionDb.unaccept(record.sourceKey);
            resurfaced++;
        }
    }
    suggestionLog.info(`recommendAgain: ${resurfaced} accepted suggestion(s) whose entry was deleted will be suggested again`);
}

/* ── GET issueDetails?taskId= ── */

export async function loadIssueDetails(taskId: string): Promise<IssueDetails> {
    try {
        const details = await jiraIssueDetails(taskId);
        return details ?? { summary: "", type: "", status: "" };
    } catch (error) {
        if (error instanceof AssPiHttpError) return { summary: "", type: "", status: "" };
        throw error;
    }
}
