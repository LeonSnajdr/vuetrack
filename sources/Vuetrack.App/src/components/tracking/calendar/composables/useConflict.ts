import type { ConflictTask, TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useChangeSet } from "./useChangeSet";
import { useEventCommit } from "./useEventCommit";
import { useEventMutation } from "./useEventMutation";

export function useConflict() {
    const calendarStore = useCalendarStore();
    const changeSet = useChangeSet();
    const commit = useEventCommit();
    const mutation = useEventMutation();

    const { task, events } = storeToRefs(calendarStore);

    const conflictTask = computed<ConflictTask | null>(() => {
        if (task.value.kind !== "conflict") return null;
        return task.value;
    });

    // The event the automatic resolutions work on. Starts out as the event that
    // caused the conflict and follows the user's last pointer pick.
    const selectedEvent = computed<TimeEntryEvent | null>(() => {
        const current = conflictTask.value;
        if (!current) return null;

        const selected = events.value.find((event) => event.uiId === current.selectedUiId);
        return selected ?? current.event;
    });

    const select = (event: TimeEntryEvent) => {
        const current = conflictTask.value;
        if (!current) return;

        current.selectedUiId = event.uiId;
    };

    // A resolution works on the state the user is looking at, so manual
    // adjustments and earlier resolutions stay in place. Only an attempt that
    // found no solution is rolled back.
    const previewStrategy = (resolve: () => boolean): boolean => {
        if (!conflictTask.value) return false;

        const taken = changeSet.snapshot();
        const resolved = resolve();
        if (resolved) return true;

        changeSet.restore(taken);
        return false;
    };

    const apply = async () => {
        if (!conflictTask.value) return;
        await commit.commitStaged();
    };

    const cancel = () => {
        const current = conflictTask.value;
        if (!current) return;

        task.value = { kind: "none" };

        changeSet.revertAll();
        if (current.event.kind === "draft") mutation.removeDraftEvent(current.event.uiId);
    };

    return { selectedEvent, select, previewStrategy, apply, cancel };
}
