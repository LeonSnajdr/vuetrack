import type { TimeEntryEvent } from "@/components/tracking/calendar/types";

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

export const canStartInteraction = (currentKind: string): boolean => {
    return currentKind !== "create" && currentKind !== "edit" && currentKind !== "conflict";
};
