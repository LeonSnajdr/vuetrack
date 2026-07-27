import type { CreatableEvent } from "@/components/tracking/calendar/types";
import { useCalendarHelper } from "./useCalendarHelper";
import { useChangeSet } from "./useChangeSet";
import { useEventCommit } from "./useEventCommit";
import { useEventMutation } from "./useEventMutation";

export function useCreate() {
    const calendarStore = useCalendarStore();
    const changeSet = useChangeSet();
    const commit = useEventCommit();
    const mutation = useEventMutation();
    const { buildCreatePayload } = useCalendarHelper();

    const { task } = storeToRefs(calendarStore);

    const start = (event: CreatableEvent) => {
        const payload = buildCreatePayload(event);
        task.value = { kind: "create", event, payload };
    };

    const finish = async () => {
        if (task.value.kind !== "create") return;

        const { event, payload } = task.value;

        changeSet.stageAdd(event, payload);
        await commit.commitOrEscalate(event);
    };

    // Abandons the whole batch, not just this form: a create task can be the
    // recovery step of a rejected conflict resolution.
    const cancel = () => {
        if (task.value.kind !== "create") return;

        const { event } = task.value;
        task.value = { kind: "none" };

        changeSet.revertAll();
        if (event.kind === "draft") mutation.removeDraftEvent(event.uiId);
    };

    return { start, finish, cancel };
}
