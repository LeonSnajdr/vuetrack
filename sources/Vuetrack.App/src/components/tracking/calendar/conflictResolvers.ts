import type { EventPosition, TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useDateHelper } from "@/composables/useDateHelper";

export type Occupied = {
    event: TimeEntryEvent;
    position: EventPosition;
};

export type Proposal =
    | { kind: "move"; event: TimeEntryEvent; position: EventPosition }
    | { kind: "remove"; event: TimeEntryEvent }
    | { kind: "split"; event: TimeEntryEvent; head: EventPosition; tail: EventPosition };

export type ConflictResolver = (subject: Occupied, occupied: Occupied[]) => Proposal[] | null;

const { startOfDay, addDays } = useDateHelper();

const isOverlapping = (subject: EventPosition, other: EventPosition): boolean => {
    return subject.start < other.end && subject.end > other.start;
};

const getSearchWindow = (subject: Occupied) => {
    const windowStart = startOfDay(new Date(subject.position.start)).getTime();
    const lastOccupiedMs = Math.max(subject.position.start, subject.position.end - 1);
    const lastOccupiedDay = startOfDay(new Date(lastOccupiedMs));
    const windowEndExclusive = addDays(lastOccupiedDay, 1).getTime();

    return { windowStart, windowEndExclusive };
};

const getSearchCandidates = (subject: Occupied, occupied: Occupied[]) => {
    const { windowStart, windowEndExclusive } = getSearchWindow(subject);
    const candidates = [...occupied]
        .filter((candidate) => candidate.event.uiId !== subject.event.uiId)
        .sort((a, b) => a.position.start - b.position.start)
        .filter((candidate) => candidate.position.end > windowStart && candidate.position.start < windowEndExclusive);

    return { candidates, windowStart, windowEndExclusive };
};

const getOverlaps = (subject: Occupied, occupied: Occupied[]): Occupied[] => {
    return occupied.filter((candidate) => candidate.event.uiId !== subject.event.uiId).filter((candidate) => isOverlapping(subject.position, candidate.position));
};

export const resolveShiftUp: ConflictResolver = (subject, occupied) => {
    const duration = subject.position.end - subject.position.start;
    const { candidates, windowStart, windowEndExclusive } = getSearchCandidates(subject, occupied);

    let searchTime = subject.position.start;

    while (searchTime >= windowStart) {
        const potentialStart = searchTime;
        const potentialEnd = searchTime + duration;

        if (potentialEnd > windowEndExclusive) {
            searchTime = windowEndExclusive - duration;
            continue;
        }

        const overlap = candidates.find((candidate) => potentialStart < candidate.position.end && potentialEnd > candidate.position.start);
        if (!overlap) return [{ kind: "move", event: subject.event, position: { start: potentialStart, end: potentialEnd } }];

        searchTime = overlap.position.start - duration;
    }

    return null;
};

export const resolveShiftDown: ConflictResolver = (subject, occupied) => {
    const duration = subject.position.end - subject.position.start;
    const { candidates, windowEndExclusive } = getSearchCandidates(subject, occupied);

    let searchTime = subject.position.start;

    while (searchTime + duration <= windowEndExclusive) {
        const overlap = candidates.find((candidate) => searchTime < candidate.position.end && searchTime + duration > candidate.position.start);
        if (!overlap) return [{ kind: "move", event: subject.event, position: { start: searchTime, end: searchTime + duration } }];

        searchTime = overlap.position.end;
    }

    return null;
};

export const resolveTruncate: ConflictResolver = (subject, occupied) => {
    const overlaps = getOverlaps(subject, occupied);
    const { start, end } = subject.position;

    const isFullyContained = overlaps.some((overlap) => overlap.position.start <= start && overlap.position.end >= end);
    if (isFullyContained) return null;

    let allowedStart = start;
    let allowedEnd = end;

    overlaps.forEach((overlap) => {
        if (overlap.position.start > start && overlap.position.start < end) {
            allowedEnd = Math.min(allowedEnd, overlap.position.start);
        }
        if (overlap.position.end > start && overlap.position.end < end) {
            allowedStart = Math.max(allowedStart, overlap.position.end);
        }
    });

    if (allowedEnd <= allowedStart) return null;

    return [{ kind: "move", event: subject.event, position: { start: allowedStart, end: allowedEnd } }];
};

export const resolveForce: ConflictResolver = (subject, occupied) => {
    const overlaps = getOverlaps(subject, occupied);
    const { start, end } = subject.position;
    const proposals: Proposal[] = [];

    for (const overlap of overlaps) {
        const other = overlap.position;

        // Completely overlapped - remove it
        if (start <= other.start && end >= other.end) {
            proposals.push({ kind: "remove", event: overlap.event });
            continue;
        }

        // Event sits inside the overlap - split it: head shrinks, tail becomes a new entry
        if (start > other.start && end < other.end) {
            proposals.push({ kind: "split", event: overlap.event, head: { start: other.start, end: start }, tail: { start: end, end: other.end } });
            continue;
        }

        // Partially overlapped - truncate it
        if (start > other.start && start < other.end) {
            proposals.push({ kind: "move", event: overlap.event, position: { start: other.start, end: start } });
        }

        if (end > other.start && end < other.end) {
            proposals.push({ kind: "move", event: overlap.event, position: { start: end, end: other.end } });
        }
    }

    return proposals;
};
