import type { TimeEntryCreateContract } from "@/contracts/TimeEntryContract";
import type { Nullable } from "@/util/Nullable";
import { maxBy } from "lodash";

export function useTimeEntryHelper() {
    const trackingStore = useTrackingStore();
    const timeEntryStore = useTimeEntryStore();
    const { timeEntries } = storeToRefs(timeEntryStore);
    const { preselectedTaskId } = storeToRefs(trackingStore);

    const createDefaultTimeEntry = (defaultValues?: Partial<Nullable<TimeEntryCreateContract>>): Nullable<TimeEntryCreateContract> => {
        const newestTimeEntry = maxBy(timeEntries.value, (x) => x.endTime);

        return {
            taskId: preselectedTaskId.value ?? null,
            startTime: defaultValues?.startTime ?? newestTimeEntry?.endTime ?? new Date(new Date().setHours(8)),
            endTime: defaultValues?.endTime ?? null,
            activityId: null,
            projectId: null,
            comment: null
        };
    };

    return { createDefaultTimeEntry };
}
