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

    const start = (event: PositionableEvent) => {
        cancelPendingUpdateForEvent(event);

        // Staged up front so the date fields the form writes into are rolled
        // back correctly when the user cancels.
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

    // Abandons the whole batch, not just this form: an edit task can be the
    // recovery step of a rejected conflict resolution.
    const cancel = () => {
        if (task.value.kind !== "edit") return;

        task.value = { kind: "none" };
        changeSet.revertAll();
    };

    return { start, finish, cancel };
}
