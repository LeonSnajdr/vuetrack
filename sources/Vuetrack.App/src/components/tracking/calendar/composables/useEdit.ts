import type { PositionableEvent } from "@/components/tracking/calendar/types";
import { useCalendarHelper } from "./useCalendarHelper";
import { useChangeSet } from "./useChangeSet";
import { useEventCommit } from "./useEventCommit";

export function useEdit() {
    const calendarStore = useCalendarStore();
    const changeSet = useChangeSet();
    const commit = useEventCommit();
    const { cancelPendingUpdateForEvent } = useCalendarHelper();

    const { task } = storeToRefs(calendarStore);

    // The form edits the staged proposal, so a typed time moves the box straight away.
    // Editing something staged for removal would commit that removal instead, and a
    // pending create belongs to the create form.
    const start = (event: PositionableEvent) => {
        if (changeSet.isRemoved(event.uiId)) return;
        if (changeSet.get(event.uiId)?.kind === "create") return;

        cancelPendingUpdateForEvent(event);

        const change = changeSet.stageSave(event);
        task.value = { kind: "edit", event, payload: change.payload };
    };

    const finish = async () => {
        if (task.value.kind !== "edit") return;

        const { event } = task.value;
        await commit.commitOrEscalate(event);
    };

    const cancel = () => {
        if (task.value.kind !== "edit") return;

        task.value = { kind: "none" };
        changeSet.revertAll();
    };

    return { start, finish, cancel };
}
