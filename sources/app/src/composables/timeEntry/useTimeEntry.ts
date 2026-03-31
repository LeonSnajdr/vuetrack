import type { TimeEntryCreateContract } from "@/contracts/TimeEntryContract";
import { maxBy } from "lodash";

export function useTimeEntry() {
    const timeEntryStore = useTimeEntryStore();
    const { timeEntries } = storeToRefs(timeEntryStore);

    const newestTimeEntry = computed(() => {
        return maxBy(timeEntries.value, (x) => x.endTime);
    });

    const createDefaultTimeEntry = (defaultValues?: Partial<TimeEntryCreateContract>): TimeEntryCreateContract => {
        return {
            taskId: "",
            startTime: defaultValues?.startTime ?? newestTimeEntry.value?.endTime ?? new Date(),
            endTime: defaultValues?.endTime ?? new Date(),
            activityId: null,
            projectId: null,
            comment: ""
        };
    };

    return { createDefaultTimeEntry, newestTimeEntry };
}
