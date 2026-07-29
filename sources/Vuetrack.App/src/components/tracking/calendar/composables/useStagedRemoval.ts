import type { TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useChangeSet } from "./useChangeSet";
import { useEventPolicy } from "./useEventPolicy";

// Stages the removal so it can be brought back; nothing is sent until Apply.
export function useStagedRemoval() {
    const changeSet = useChangeSet();
    const policy = useEventPolicy();

    const isStaged = (event: TimeEntryEvent): boolean => {
        return changeSet.isRemoved(event.uiId);
    };

    const toggle = (event: TimeEntryEvent): void => {
        if (isStaged(event)) {
            changeSet.restoreRemoved(event.uiId);
            return;
        }

        if (!policy.canStageRemoval(event)) return;

        changeSet.stageRemove(event);
    };

    return { isStaged, toggle };
}
