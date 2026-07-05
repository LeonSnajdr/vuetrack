/*
 * HTML/DOM parsing for AssPI — the HTML-scraping fake backend.
 *
 * Ported near-verbatim from a colleague's Tampermonkey userscript that drove the
 * legacy timetracking site (`https://timetracking.cloud.samhammer.de`). Only the
 * parsing and pure helpers are kept; the userscript's Jira/UI layers are dropped.
 *
 * These functions run in the browser and rely on the global `DOMParser`.
 */

// Selector for the project <select> on the legacy /tracking/show form.
const PROJECT_SELECT_SELECTOR = '#project_id, select[name="project.id"]';

// Match server-rendered entry rows on the legacy list page, most-specific first.
const ENTRY_ROWS_SELECTOR = "table tbody tr[data-time-entry-id], table tbody tr[id^=\"entry\"], table tbody tr";

export type LegacyOption = {
    id: string;
    name: string;
};

export type LegacyShowForm = {
    csrfToken: string;
    userId: string;
    createdByUserId: string;
    projectOptions: LegacyOption[];
    selectedProjectId: string;
    activityOptions: LegacyOption[];
    selectedActivityId: string;
    startDate: string;
    startTime: string;
    endDate: string;
    endTime: string;
    taskId: string;
    comment: string;
    approved: boolean;
};

export type LegacyEntryRow = {
    id: string;
    start: Date;
    end: Date;
    taskId: string;
};

/* ── pure formatters/helpers ── */

export const pad = (n: number): string => String(n).padStart(2, "0");

// Legacy German date format used in URLs and form fields, e.g. "03.07.2026".
export const fmtDate = (d: Date): string => `${pad(d.getDate())}.${pad(d.getMonth() + 1)}.${d.getFullYear()}`;

export const fmtTime = (d: Date): string => `${pad(d.getHours())}:${pad(d.getMinutes())}`;

export const minsToHM = (mins: number): string => `${pad(Math.floor(mins / 60))}:${pad(mins % 60)}`;

export function parseGermanDate(str: string): Date | null {
    const m = /(\d{2})\.(\d{2})\.(\d{4})/.exec(str || "");
    return m ? new Date(+m[3], +m[2] - 1, +m[1]) : null;
}

function optionsOf(selectEl: HTMLSelectElement): LegacyOption[] {
    return [...selectEl.querySelectorAll("option")]
        .map((o) => ({ id: o.value, name: (o.textContent || "").trim() }))
        .filter((o) => o.id && o.name);
}

function findProjectSelect(doc: Document): HTMLSelectElement | null {
    let sel = doc.querySelector<HTMLSelectElement>(PROJECT_SELECT_SELECTOR);
    if (sel && optionsOf(sel).length) return sel;

    const selects = [...doc.querySelectorAll("select")];
    sel = selects.find((s) => /project|projekt|kostenstelle/i.test((s.name || "") + " " + (s.id || ""))) ?? null;
    if (sel && optionsOf(sel).length) return sel;

    // Fall back to the select with the most options — usually the project list.
    let best: HTMLSelectElement | null = null;
    let bestN = 1;
    for (const s of selects) {
        const n = optionsOf(s).length;
        if (n > bestN) {
            best = s;
            bestN = n;
        }
    }
    return best;
}

function findActivitySelect(doc: Document, projectSelect: HTMLSelectElement | null): HTMLSelectElement | null {
    const selects = [...doc.querySelectorAll("select")];
    const sel = selects.find((s) => /activit|t[aä]tigkeit/i.test((s.name || "") + " " + (s.id || "")));
    if (sel) return sel;
    return selects.find((s) => s !== projectSelect && optionsOf(s).length) ?? null;
}

function selectedValueOf(sel: HTMLSelectElement | null): string {
    if (!sel) return "";
    // DOMParser documents do not set sel.value from parsed HTML, so we must read
    // the [selected] attribute. Fall back to the first option if none is selected.
    const marked = sel.querySelector<HTMLOptionElement>("option[selected]");
    if (marked) return marked.value;
    const first = sel.querySelector<HTMLOptionElement>("option");
    return first ? first.value : "";
}

// The legacy /tracking/activities endpoint returns JSON in a few possible shapes.
export function parseActivities(json: unknown): LegacyOption[] {
    if (Array.isArray(json)) {
        return json
            .map((a) => ({ id: String(a.id ?? a.activityId ?? a.value ?? ""), name: String(a.name ?? a.text ?? a.label ?? "") }))
            .filter((a) => a.id);
    }
    if (json && typeof json === "object") {
        return Object.entries(json as Record<string, unknown>).map(([id, name]) => ({ id, name: String(name) }));
    }
    return [];
}

/* ── HTML page parsers ── */

export class HtmlParser {
    // Parse the /tracking/show form: csrf token, user ids, project/activity
    // options + current selection, and the current field values.
    static parseShowForm(html: string): LegacyShowForm {
        const doc = new DOMParser().parseFromString(html, "text/html");
        const val = (name: string): string => {
            const el = doc.querySelector<HTMLInputElement>(`[name="${CSS.escape(name)}"]`);
            return el ? (el.value ?? el.getAttribute("value") ?? "") : "";
        };

        const projSel = findProjectSelect(doc);
        const projectOptions = projSel ? optionsOf(projSel) : [];
        const actSel = findActivitySelect(doc, projSel);
        const activityOptions = actSel ? optionsOf(actSel) : [];

        return {
            csrfToken: val("csrfToken"),
            userId: val("userId") || "",
            createdByUserId: val("createdByUserId") || val("userId") || "",
            projectOptions,
            selectedProjectId: selectedValueOf(projSel),
            activityOptions,
            selectedActivityId: selectedValueOf(actSel),
            startDate: val("startDate"),
            startTime: val("startTime"),
            endDate: val("endDate"),
            endTime: val("endTime"),
            taskId: val("taskId"),
            comment: val("comment"),
            approved: val("approved") === "true"
        };
    }

    static parseEntryList(html: string): LegacyEntryRow[] {
        const doc = new DOMParser().parseFromString(html, "text/html");
        return HtmlParser.listEntries(doc);
    }

    static listEntries(doc: Document): LegacyEntryRow[] {
        const out: LegacyEntryRow[] = [];
        doc.querySelectorAll(ENTRY_ROWS_SELECTOR).forEach((tr) => {
            // Each row is parsed in isolation: one broken row must never abort the
            // whole list and leave the calendar blank.
            try {
                const id =
                    tr.getAttribute("data-time-entry-id") ||
                    (tr.id && tr.id.replace(/\D/g, "")) ||
                    ((tr.getAttribute("onclick") || "").match(/timeEntryId=(\d+)/) || [])[1];
                if (!id) return;

                const text = tr.textContent || "";
                const dateM = text.match(/\d{2}\.\d{2}\.\d{4}/);
                const times = text.match(/\b(\d{2}:\d{2})\b/g);
                if (!dateM || !times || times.length < 2) return;

                const day = parseGermanDate(dateM[0]);
                if (!day) return;

                const start = new Date(day);
                const [sh, sm] = times[0].split(":");
                start.setHours(+sh, +sm, 0, 0);

                const end = new Date(day);
                const [eh, em] = times[1].split(":");
                end.setHours(+eh, +em, 0, 0);

                let taskId = "";
                try {
                    taskId = (text.match(/\b[A-Za-z][A-Za-z0-9]+-\d+\b/) || [""])[0];
                } catch {
                    /* best-effort; entry still usable without a task id */
                }

                out.push({ id, start, end, taskId });
            } catch {
                /* swallow and continue with the next row */
            }
        });
        return out;
    }
}
