import type { PositionableEvent } from "@/components/tracking/calendar/types";
import { useCalendarHelper } from "./useCalendarHelper";
import { useChangeSet } from "./useChangeSet";
import { useEventCommit } from "./useEventCommit";

export function useEdit() {
    const calendarStore = useCalendarStore();
    const changeSet = useChangeSet();
    const commit = useEventCommit();
    const { buildUpdatePayload, cancelPendingUpdateForEvent } = useCalendarHelper();

    const { task } = storeToRefs(calendarStore);

    // Editing something staged for removal would commit that removal instead.
    const start = (event: PositionableEvent) => {
        if (changeSet.isRemoved(event.uiId)) return;

        cancelPendingUpdateForEvent(event);

        changeSet.stageUpdate(event);

        const payload = buildUpdatePayload(event);
        task.value = { kind: "edit", event, payload };
    };

    const finish = async () => {
        if (task.value.kind !== "edit") return;

        const { event, payload } = task.value;

        changeSet.stageUpdate(event, payload);
        await commit.commitOrEscalate(event);
    };

    const cancel = () => {
        if (task.value.kind !== "edit") return;

        task.value = { kind: "none" };
        changeSet.revertAll();
    };

    return { start, finish, cancel };
}
