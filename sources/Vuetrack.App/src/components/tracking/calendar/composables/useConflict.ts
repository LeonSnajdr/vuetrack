import type { ConflictTask, TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useChangeSet } from "./useChangeSet";
import { useEventCommit } from "./useEventCommit";
import { useEventSelection } from "./useEventSelection";

export function useConflict() {
    const calendarStore = useCalendarStore();
    const changeSet = useChangeSet();
    const commit = useEventCommit();
    const selection = useEventSelection();

    const { task } = storeToRefs(calendarStore);

    const conflictTask = computed<ConflictTask | null>(() => {
        if (task.value.kind !== "conflict") return null;
        return task.value;
    });

    const selectedEvent = computed<TimeEntryEvent | null>(() => {
        const current = conflictTask.value;
        if (!current) return null;

        return selection.selectedEvent.value ?? current.event;
    });

    // Builds on the current state; only a failed attempt is rolled back.
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

        selection.clearSelection();
        await commit.commitStaged();
    };

    const cancel = () => {
        if (!conflictTask.value) return;

        task.value = { kind: "none" };
        selection.clearSelection();

        changeSet.revertAll();
    };

    return { selectedEvent, previewStrategy, apply, cancel };
}
