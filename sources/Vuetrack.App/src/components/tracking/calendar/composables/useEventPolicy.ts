import type { PositionableEvent, SuggestionTimeEntryEvent, TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useChangeSet } from "./useChangeSet";

export function useEventPolicy() {
    const calendarStore = useCalendarStore();
    const changeSet = useChangeSet();

    const { task } = storeToRefs(calendarStore);

    const isIdle = computed(() => task.value.kind === "none");
    const isConflict = computed(() => task.value.kind === "conflict");

    const canOpenTask = (): boolean => {
        return isIdle.value;
    };

    const canStartGesture = (event: TimeEntryEvent): boolean => {
        if (changeSet.isRemoved(event.uiId)) return false;
        if (isIdle.value) return true;

        return isConflict.value;
    };

    const canStageRemoval = (event: TimeEntryEvent): boolean => {
        if (task.value.kind !== "conflict") return false;
        return event.uiId !== task.value.event.uiId;
    };

    const isSaved = (event: TimeEntryEvent): event is PositionableEvent => {
        return event.kind !== "draft";
    };

    const canAccept = (event: TimeEntryEvent): event is SuggestionTimeEntryEvent => {
        return event.kind === "suggestion";
    };

    return { isIdle, isConflict, canOpenTask, canStartGesture, canStageRemoval, isSaved, canAccept };
}
