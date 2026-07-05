/*
 * AssPI endpoint handlers — one per API route the vuetrack UI calls. Each handler
 * drives the legacy timetracking site (via AssPiClient) and maps the scraped HTML
 * into the exact DTO/contract shapes the real backend would return.
 *
 * Request/param building mirrors the userscript's TimetrackingService.
 */

import type { ActivityContract, ActivityId } from "@/contracts/ActivityContract";
import type { ProjectContract, ProjectId } from "@/contracts/ProjectContract";
import client, { AssPiHttpError } from "@/plugins/asspi/AssPiClient";
import { fmtDate, fmtTime, HtmlParser, minsToHM, parseActivities, type LegacyOption, type LegacyShowForm } from "@/plugins/asspi/HtmlParser";

// The wire shape the real GET /timeEntry returns and POST /timeEntry/upsert accepts.
// Kept in sync with the private TimeEntryDTO in services/TimeEntryService.ts.
export type TimeEntryDTO = {
    timeEntryId: number | null;
    userId: number;
    createdByUserId: number | null;
    project: { id: number; name: string };
    activity: { id: number; name: string };
    breakDetails: { durationMillis: number; valid: boolean } | null;
    taskId: string;
    startDate: string;
    startTime: string;
    endDate: string;
    endTime: string;
    comment: string;
    approved: boolean;
};

// Fetch at most this many per-entry detail forms concurrently.
const DETAIL_CONCURRENCY = 6;

function nameOf(options: LegacyOption[], id: string): string {
    return options.find((o) => o.id === id)?.name ?? "";
}

function combineToDate(dmY: string, hm: string): Date | null {
    const dateM = /(\d{2})\.(\d{2})\.(\d{4})/.exec(dmY || "");
    if (!dateM) return null;
    const [hh = "0", mm = "0"] = (hm || "").split(":");
    return new Date(+dateM[3], +dateM[2] - 1, +dateM[1], +hh, +mm, 0, 0);
}

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

async function getShowForm(timeEntryId?: string): Promise<LegacyShowForm> {
    const path = timeEntryId ? `/tracking/show?timeEntryId=${encodeURIComponent(timeEntryId)}` : "/tracking/show";
    const r = await client.getPage(path);
    if (!r.ok) throw new AssPiHttpError(r.status);
    return HtmlParser.parseShowForm(r.text);
}

/* ── GET timeEntry?from&to ── */

export async function loadTimeEntries(from: string, to: string): Promise<TimeEntryDTO[]> {
    const q = `/?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}&searchterm=&filter=`;
    const r = await client.getPage(q);
    if (!r.ok) throw new AssPiHttpError(r.status);
    const rows = HtmlParser.parseEntryList(r.text);

    // N+1: the list rows lack project/activity/comment/approved, so pull each
    // entry's show form. Bounded concurrency keeps the legacy site happy.
    return mapLimited(rows, DETAIL_CONCURRENCY, async (row) => {
        const form = await getShowForm(row.id);

        const startDate = form.startDate || fmtDate(row.start);
        const startTime = form.startTime || fmtTime(row.start);
        const endDate = form.endDate || fmtDate(row.end);
        const endTime = form.endTime || fmtTime(row.end);

        return {
            timeEntryId: Number(row.id),
            userId: Number(form.userId) || 0,
            createdByUserId: form.createdByUserId ? Number(form.createdByUserId) : null,
            project: { id: Number(form.selectedProjectId) || 0, name: nameOf(form.projectOptions, form.selectedProjectId) },
            activity: { id: Number(form.selectedActivityId) || 0, name: nameOf(form.activityOptions, form.selectedActivityId) },
            breakDetails: null,
            taskId: form.taskId || row.taskId,
            startDate,
            startTime,
            endDate,
            endTime,
            comment: form.comment,
            approved: form.approved
        } satisfies TimeEntryDTO;
    });
}

/* ── POST timeEntry/upsert ── */

export async function upsertTimeEntry(dto: TimeEntryDTO): Promise<void> {
    const idStr = dto.timeEntryId != null ? String(dto.timeEntryId) : "";
    const form = await getShowForm(idStr || undefined);

    const start = combineToDate(dto.startDate, dto.startTime);
    const end = combineToDate(dto.endDate, dto.endTime);
    const duration = start && end ? minsToHM(Math.max(0, Math.round((end.getTime() - start.getTime()) / 60000))) : "00:00";

    const params: Record<string, string> = {
        csrfToken: form.csrfToken,
        timeEntryId: idStr,
        approved: dto.approved ? "true" : "false",
        submitType: "",
        logout: "",
        userId: form.userId || String(dto.userId ?? ""),
        createdByUserId: form.createdByUserId || (dto.createdByUserId != null ? String(dto.createdByUserId) : ""),
        startDate: dto.startDate,
        startTime: dto.startTime,
        endDate: dto.endDate,
        endTime: dto.endTime,
        duration,
        taskId: dto.taskId || "",
        "project.id": dto.project.id ? String(dto.project.id) : "",
        "activity.id": dto.activity.id ? String(dto.activity.id) : "",
        comment: dto.comment || ""
    };

    const res = await client.postForm("/tracking/submit", params);
    if (!res.ok) throw new AssPiHttpError(res.status);
}

/* ── DELETE timeEntry ── */

export async function deleteTimeEntry(idsToDelete: string): Promise<void> {
    const r = await client.getPage(`/tracking/delete/confirm?idsToDelete=${encodeURIComponent(idsToDelete)}`);
    if (!r.ok) throw new AssPiHttpError(r.status);
    const meta = HtmlParser.parseShowForm(r.text);

    const res = await client.postForm("/tracking/delete/confirm", { csrfToken: meta.csrfToken, idsToDelete });
    if (!res.ok) throw new AssPiHttpError(res.status);
}

/* ── GET project ── */

export async function loadProjects(): Promise<ProjectContract[]> {
    const form = await getShowForm();
    return form.projectOptions.map((o) => ({ id: Number(o.id) as ProjectId, name: o.name }));
}

/* ── GET project/{id}/activity ── */

export async function loadActivities(projectId: string): Promise<ActivityContract[]> {
    const json = await client.getJson<unknown>(`/tracking/activities?projectId=${encodeURIComponent(projectId)}`);
    return parseActivities(json).map((o) => ({ id: Number(o.id) as ActivityId, name: o.name }));
}

/* ── GET project/findByTaskId ── */

// Pull a numeric project id out of the /tracking/project/info response,
// tolerating a few likely shapes (plain number, {id}, {projectId}, {project:{id}}).
function extractProjectId(text: string): number | undefined {
    const trimmed = (text || "").trim();
    if (!trimmed) return undefined;
    if (/^\d+$/.test(trimmed)) return Number(trimmed);
    try {
        const json = JSON.parse(trimmed);
        if (typeof json === "number") return Number.isFinite(json) ? json : undefined;
        if (json && typeof json === "object") {
            const candidate = json.id ?? json.projectId ?? json.project?.id ?? json["project.id"];
            const n = Number(candidate);
            return candidate != null && candidate !== "" && Number.isFinite(n) ? n : undefined;
        }
    } catch {
        /* not JSON */
    }
    return undefined;
}

// The legacy site maps a task to its project via /tracking/project/info?taskId=.
export async function findProjectIdByTaskId(taskId: string): Promise<ProjectId | undefined> {
    if (!taskId) return undefined;
    const r = await client.getPage(`/tracking/project/info?taskId=${encodeURIComponent(taskId)}`);
    if (!r.ok) return undefined; // e.g. no project mapped for this task
    const id = extractProjectId(r.text);
    return id != null ? (id as ProjectId) : undefined;
}
