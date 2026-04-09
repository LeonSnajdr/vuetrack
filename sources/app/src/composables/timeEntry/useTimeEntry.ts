import type { TimeEntryCreateContract } from "@/contracts/TimeEntryContract";
import type { Nullable } from "@/util/Nullable";
import { maxBy } from "lodash";

export function useTimeEntry() {
    const timeEntryStore = useTimeEntryStore();
    const { timeEntries } = storeToRefs(timeEntryStore);

    const createDefaultTimeEntry = (defaultValues?: Partial<Nullable<TimeEntryCreateContract>>): Nullable<TimeEntryCreateContract> => {
        const newestTimeEntry = maxBy(timeEntries.value, (x) => x.endTime);

        return {
            taskId: null,
            startTime: defaultValues?.startTime ?? newestTimeEntry?.endTime ?? null,
            endTime: defaultValues?.endTime ?? null,
            activityId: null,
            projectId: null,
            comment: null
        };
    };

    return { createDefaultTimeEntry };
}
