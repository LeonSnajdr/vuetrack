import type { TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useChangeSet } from "./useChangeSet";

// Single home for "is this allowed right now" decisions, so the calendar, the
// event component, the context menu and the shortcuts all agree.
export function useEventPolicy() {
    const calendarStore = useCalendarStore();
    const changeSet = useChangeSet();

    const { task } = storeToRefs(calendarStore);

    const isIdle = computed(() => task.value.kind === "none");
    const isManualConflict = computed(() => task.value.kind === "conflict" && task.value.mode === "manual");

    const canOpenTask = (): boolean => {
        return isIdle.value;
    };

    const canStartGesture = (event: TimeEntryEvent): boolean => {
        if (changeSet.isRemoved(event.uiId)) return false;
        if (isIdle.value) return true;
        if (task.value.kind !== "conflict") return false;
        if (isManualConflict.value) return true;

        return event.uiId === task.value.event.uiId;
    };

    // In manual mode a removal is staged instead of deleted. The event the
    // conflict is about is excluded: abandoning that one is what Cancel is for.
    const canStageRemoval = (event: TimeEntryEvent): boolean => {
        if (!isManualConflict.value) return false;
        if (task.value.kind !== "conflict") return false;
        if (event.kind === "draft") return false;

        return event.uiId !== task.value.event.uiId;
    };

    return { isIdle, isManualConflict, canOpenTask, canStartGesture, canStageRemoval };
}
