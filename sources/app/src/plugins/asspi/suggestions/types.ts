/*
 * Internal shapes for the AssPI fake suggestion backend.
 *
 * A SuggestionProvider is a pluggable source of suggestion candidates. Today the
 * only provider is Jira; Outlook/Teams event providers can be added as siblings
 * later without touching the app — the RESTful `timeEntrySuggestions` contract is
 * source-agnostic. This abstraction is an implementation detail of the fake
 * backend (and later, of a real backend), not a top-level app concept.
 */

export type DateRange = {
    from: Date;
    to: Date;
};

// A raw suggestion before project/activity resolution and time layout.
export type SuggestionCandidate = {
    // Source identifier, e.g. "jira" | "outlook" | "teams".
    source: string;
    // Stable id within the source for a given day (e.g. Jira story key).
    externalId: string;
    // ISO yyyy-MM-dd the candidate belongs to.
    date: string;
    // Task key written onto the resulting time entry.
    taskId: string;
    // Human title → suggestion comment.
    title: string;
    // Concrete time if the source has one (e.g. a real meeting). When absent the
    // backend lays the candidate out across the day's free slots.
    preferredStart?: Date;
    preferredEnd?: Date;
};

export interface SuggestionProvider {
    readonly id: string;
    getCandidates(range: DateRange): Promise<SuggestionCandidate[]>;
}

// Stable key used for dismissals and persisted positions.
export function sourceKeyOf(candidate: Pick<SuggestionCandidate, "source" | "externalId" | "date">): string {
    return `${candidate.source}:${candidate.externalId}:${candidate.date}`;
}

// Deterministic positive 31-bit id from a source key (DJB2). Stable across
// reloads so the calendar store keeps event identity, and collision-safe enough
// for a day/week of suggestions.
export function idFromSourceKey(sourceKey: string): number {
    let h = 5381;
    for (let i = 0; i < sourceKey.length; i++) {
        h = (Math.imul(h, 33) + sourceKey.charCodeAt(i)) >>> 0;
    }
    return h & 0x7fffffff;
}
