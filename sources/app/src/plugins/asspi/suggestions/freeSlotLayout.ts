/*
 * Free-slot layout for suggestions without a concrete time.
 *
 * Ported from the userscript's openActivitySuggestion: distribute N suggestions
 * across a workday's free time (08:00–17:00, minus lunch, minus already-occupied
 * slots). Pure and minute-based; the caller converts minutes-of-day to Dates.
 */

const DAY_START = 8 * 60; // 08:00
const DAY_END = 17 * 60; // 17:00
const LUNCH_START = 13 * 60;
const LUNCH_END = 14 * 60;
const SLOT = 30; // layout granularity in minutes
const MIN_DURATION = 30;

// A busy interval in minutes-from-midnight.
export type BusyInterval = { start: number; end: number };

export type SlotResult = { start: number; end: number };

const round30 = (m: number): number => Math.round(m / SLOT) * SLOT;

// Build free intervals inside the workday, excluding lunch and busy intervals.
function freeIntervals(busy: BusyInterval[]): BusyInterval[] {
    const blocked = [...busy, { start: LUNCH_START, end: LUNCH_END }].sort((a, b) => a.start - b.start);

    const free: BusyInterval[] = [];
    let cur = DAY_START;
    for (const b of blocked) {
        const bs = Math.max(b.start, DAY_START);
        const be = Math.min(b.end, DAY_END);
        if (bs > cur) free.push({ start: cur, end: Math.min(bs, DAY_END) });
        cur = Math.max(cur, be);
    }
    if (cur < DAY_END) free.push({ start: cur, end: DAY_END });
    return free;
}

// Lay out `count` suggestions across the free time. Returns one slot per index.
//
// Slots are packed back-to-back into the free intervals so they never overlap
// each other, lunch, or existing entries. Each gets an estimated duration —
// the free time spread evenly across all suggestions, snapped to the layout
// granularity and floored at the minimum booking length. If the day is too full
// to hold them all, the remainder stacks sequentially after the workday end;
// still non-overlapping, just outside 08:00–17:00.
export function layoutFreeSlots(count: number, busy: BusyInterval[]): SlotResult[] {
    if (count <= 0) return [];

    const free = freeIntervals(busy);
    const freeMin = free.reduce((sum, iv) => sum + (iv.end - iv.start), 0);

    // Estimated duration per suggestion, never below the minimum booking length.
    const duration = Math.max(MIN_DURATION, round30(freeMin / count));

    const slots: SlotResult[] = [];
    let ivIdx = 0;
    let cursor = free.length ? free[0].start : DAY_END;
    let overflow = DAY_END; // where post-workday fallback blocks begin

    for (let i = 0; i < count; i++) {
        // Skip past any free interval that can no longer hold a minimum block.
        while (ivIdx < free.length && cursor + MIN_DURATION > free[ivIdx].end) {
            ivIdx++;
            if (ivIdx < free.length) cursor = free[ivIdx].start;
        }

        if (ivIdx < free.length) {
            const start = cursor;
            const end = Math.min(start + duration, free[ivIdx].end);
            slots.push({ start, end });
            cursor = end;
        } else {
            // Day is full — stack the rest back-to-back after the workday end.
            slots.push({ start: overflow, end: overflow + duration });
            overflow += duration;
        }
    }

    return slots;
}
