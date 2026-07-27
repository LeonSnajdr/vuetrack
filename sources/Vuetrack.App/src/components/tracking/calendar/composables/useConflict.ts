import type { ConflictTask } from "@/components/tracking/calendar/types";
import { useChangeSet } from "./useChangeSet";
import { useEventCommit } from "./useEventCommit";
import { useEventMutation } from "./useEventMutation";

export function useConflict() {
    const calendarStore = useCalendarStore();
    const changeSet = useChangeSet();
    const commit = useEventCommit();
    const mutation = useEventMutation();

    const { task } = storeToRefs(calendarStore);

    const conflictTask = computed<ConflictTask | null>(() => {
        if (task.value.kind !== "conflict") return null;
        return task.value;
    });

    // Undoes everything staged while resolving but keeps the change that caused
    // the conflict: reverting that one would defeat the purpose, and for a new
    // entry it would delete the event the panel is about.
    const reset = () => {
        const current = conflictTask.value;
        if (!current) return;

        changeSet.revertAllExcept(current.event.uiId);
        current.previewStrategyId = undefined;
    };

    const previewStrategy = (strategyId: string, resolve: () => boolean): boolean => {
        const current = conflictTask.value;
        if (!current) return false;

        reset();

        const resolved = resolve();
        if (!resolved) {
            reset();
            return false;
        }

        current.previewStrategyId = strategyId;
        return true;
    };

    const enterManual = () => {
        const current = conflictTask.value;
        if (!current) return;

        current.mode = "manual";
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

    return { conflictTask, reset, previewStrategy, enterManual, apply, cancel };
}
