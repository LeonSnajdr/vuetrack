/*
 * IndexedDB storage for the AssPI fake suggestion backend — this is the fake
 * api's "database". A real backend owns the same state server-side, so nothing
 * here leaks into the app: the RESTful contract keeps its plain numeric id.
 *
 * Three object stores keyed by the stable source key (`source:externalId:date`):
 *   - dismissals: suggestions the user dismissed (filtered out on load)
 *   - accepted:   suggestions turned into a real entry (filtered out on load,
 *                 and — unlike dismissals — NOT cleared by "recommend again")
 *   - positions:  user-adjusted start/end (survive reloads)
 */

import { openDB, type DBSchema, type IDBPDatabase } from "idb";

type StoredPosition = { start: number; end: number };

interface SuggestionSchema extends DBSchema {
    dismissals: { key: string; value: { sourceKey: string; dismissedAt: number } };
    accepted: { key: string; value: { sourceKey: string; taskId: string; date: string; acceptedAt: number } };
    positions: { key: string; value: { sourceKey: string; start: number; end: number } };
}

const DB_NAME = "vuetrack-suggestions";
const DB_VERSION = 2;

let dbPromise: Promise<IDBPDatabase<SuggestionSchema>> | null = null;

function db(): Promise<IDBPDatabase<SuggestionSchema>> {
    if (!dbPromise) {
        dbPromise = openDB<SuggestionSchema>(DB_NAME, DB_VERSION, {
            upgrade(database) {
                if (!database.objectStoreNames.contains("dismissals")) database.createObjectStore("dismissals", { keyPath: "sourceKey" });
                if (!database.objectStoreNames.contains("accepted")) database.createObjectStore("accepted", { keyPath: "sourceKey" });
                if (!database.objectStoreNames.contains("positions")) database.createObjectStore("positions", { keyPath: "sourceKey" });
            }
        });
    }
    return dbPromise;
}

export const SuggestionDb = {
    async getDismissedKeys(): Promise<Set<string>> {
        const rows = await (await db()).getAll("dismissals");
        return new Set(rows.map((r) => r.sourceKey));
    },

    async dismiss(sourceKey: string): Promise<void> {
        await (await db()).put("dismissals", { sourceKey, dismissedAt: Date.now() });
    },

    // Undo a single dismissal.
    async undismiss(sourceKey: string): Promise<void> {
        await (await db()).delete("dismissals", sourceKey);
    },

    // Hard "recommend again": clear all dismissals so past suggestions resurface.
    // Accepted suggestions are intentionally left in place — an entry already
    // exists for them, so they must not be suggested again.
    async clearDismissals(): Promise<void> {
        await (await db()).clear("dismissals");
    },

    async getAcceptedKeys(): Promise<Set<string>> {
        const rows = await (await db()).getAll("accepted");
        return new Set(rows.map((r) => r.sourceKey));
    },

    // Full accepted records, so "recommend again" can reconcile them against the
    // real entries (task id + day identify the entry that must still exist).
    async getAcceptedRecords(): Promise<{ sourceKey: string; taskId: string; date: string }[]> {
        const rows = await (await db()).getAll("accepted");
        return rows.map((r) => ({ sourceKey: r.sourceKey, taskId: r.taskId, date: r.date }));
    },

    // Mark a suggestion as accepted (turned into a real time entry). Survives
    // "recommend again" while its entry exists; the task id + day let it be
    // reconciled away if that entry is later deleted.
    async accept(sourceKey: string, taskId: string, date: string): Promise<void> {
        await (await db()).put("accepted", { sourceKey, taskId, date, acceptedAt: Date.now() });
    },

    // Drop an accepted record so the suggestion can be produced again (used when
    // its time entry no longer exists).
    async unaccept(sourceKey: string): Promise<void> {
        await (await db()).delete("accepted", sourceKey);
    },

    async getPositions(): Promise<Map<string, StoredPosition>> {
        const rows = await (await db()).getAll("positions");
        return new Map(rows.map((r) => [r.sourceKey, { start: r.start, end: r.end }]));
    },

    async savePosition(sourceKey: string, position: StoredPosition): Promise<void> {
        await (await db()).put("positions", { sourceKey, start: position.start, end: position.end });
    }
};
