import type { Interaction, TimeEntryEvent } from "@/components/tracking/calendar/types";

export const roundTime = (time: number, down = true): number => {
    const roundTo = 15;
    const step = roundTo * 60 * 1000;
    return down ? time - (time % step) : time + (step - (time % step));
};

export const getOverlappingEvents = (subject: TimeEntryEvent, candidates: TimeEntryEvent[]): TimeEntryEvent[] => {
    return candidates.filter((other) => {
        if (other.uiId === subject.uiId) return false;
        return subject.start < other.end && subject.end > other.start;
    });
};

export const getOriginalPositon = (event: TimeEntryEvent, cur: Interaction) => {
    return cur.kind === "conflict" && cur.event.uiId === event.uiId && "originalPosition" in cur.mutation
        ? cur.mutation.originalPosition
        : { start: event.start, end: event.end };
};

export const cancelPendingUpdateForEvent = (
    event: TimeEntryEvent,
    timeEntryStore: ReturnType<typeof useTimeEntryStore>,
    suggestionStore: ReturnType<typeof useTimeEntrySuggestionStore>
): void => {
    if (event.kind === "existing") {
        timeEntryStore.cancelPendingUpdate(event.timeEntry.id);
    } else if (event.kind === "suggestion") {
        suggestionStore.cancelPendingUpdate(event.timeEntry.id);
    }
};
