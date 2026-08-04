import type { CreatableEvent } from "@/components/tracking/calendar/types";
import { useChangeSet } from "./useChangeSet";
import { useEventCommit } from "./useEventCommit";

export function useCreate() {
    const calendarStore = useCalendarStore();
    const changeSet = useChangeSet();
    const commit = useEventCommit();

    const { task } = storeToRefs(calendarStore);

    const start = (event: CreatableEvent) => {
        if (changeSet.isRemoved(event.uiId)) return;

        const change = changeSet.stageCreate(event);
        task.value = { kind: "create", event, payload: change.payload };
    };

    const finish = async () => {
        if (task.value.kind !== "create") return;

        const { event } = task.value;
        await commit.commitOrEscalate(event);
    };

    const cancel = () => {
        if (task.value.kind !== "create") return;

        task.value = { kind: "none" };
        changeSet.revertAll();
    };

    return { start, finish, cancel };
}
