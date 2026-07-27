import type { TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useChangeSet } from "./useChangeSet";
import { useEventCommit } from "./useEventCommit";

export function useDelete() {
    const calendarStore = useCalendarStore();
    const changeSet = useChangeSet();
    const commit = useEventCommit();

    const { task } = storeToRefs(calendarStore);

    const start = (event: TimeEntryEvent) => {
        task.value = { kind: "delete", event };
    };

    const finish = async () => {
        if (task.value.kind !== "delete") return;

        const { event } = task.value;

        changeSet.stageRemove(event);
        await commit.commitStaged();
    };

    const cancel = () => {
        if (task.value.kind !== "delete") return;
        task.value = { kind: "none" };
    };

    return { start, finish, cancel };
}
