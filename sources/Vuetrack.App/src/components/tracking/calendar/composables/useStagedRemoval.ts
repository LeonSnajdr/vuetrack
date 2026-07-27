import type { TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useChangeSet } from "./useChangeSet";
import { useEventPolicy } from "./useEventPolicy";

// Deleting during a manual conflict resolution only stages the removal: the
// event keeps rendering so it can be brought back, and nothing is sent until
// the whole resolution is applied.
export function useStagedRemoval() {
    const changeSet = useChangeSet();
    const policy = useEventPolicy();

    const isStaged = (event: TimeEntryEvent): boolean => {
        return changeSet.isRemoved(event.uiId);
    };

    const toggle = (event: TimeEntryEvent): void => {
        if (isStaged(event)) {
            changeSet.unstage(event.uiId);
            return;
        }

        if (!policy.canStageRemoval(event)) return;

        changeSet.stageRemove(event);
    };

    return { isStaged, toggle };
}
