import type { TimeEntryCreateContract } from "@/contracts/TimeEntryContract";
import { maxBy } from "lodash";

export function useTimeEntry() {
    const timeEntryStore = useTimeEntryStore();
    const { timeEntries } = storeToRefs(timeEntryStore);

    const createDefaultTimeEntry = (defaultValues?: Partial<TimeEntryCreateContract>): TimeEntryCreateContract => {
        const newestTimeEntry = maxBy(timeEntries.value, (x) => x.endTime);

        return {
            taskId: "",
            startTime: defaultValues?.startTime ?? newestTimeEntry?.endTime ?? new Date(),
            endTime: defaultValues?.endTime ?? new Date(),
            activityId: null,
            projectId: null,
            comment: ""
        };
    };

    return { createDefaultTimeEntry };
}
