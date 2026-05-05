import type { TimeEntryCreateContract } from "@/contracts/TimeEntryContract";
import type { Nullable } from "@/util/Nullable";
import { maxBy } from "lodash";

export function useTimeEntryHelper() {
    const presetStore = usePresetStore();
    const timeEntryStore = useTimeEntryStore();
    const { timeEntries } = storeToRefs(timeEntryStore);
    const { activePreset } = storeToRefs(presetStore);

    const createDefaultTimeEntry = (defaultValues?: Partial<Nullable<TimeEntryCreateContract>>): Nullable<TimeEntryCreateContract> => {
        const newestTimeEntry = maxBy(timeEntries.value, (x) => x.endTime);
        const resolvedStartTime = defaultValues?.startTime ?? newestTimeEntry?.endTime ?? new Date(new Date().setHours(8, 0, 0, 0));
        const presetDurationMinutes = activePreset.value?.durationMinutes;
        const resolvedEndTime =
            defaultValues?.endTime ??
            (resolvedStartTime instanceof Date && presetDurationMinutes != null
                ? new Date(resolvedStartTime.getTime() + presetDurationMinutes * 60 * 1000)
                : null);

        return {
            taskId: defaultValues?.taskId ?? activePreset.value?.taskId ?? null,
            startTime: resolvedStartTime,
            endTime: resolvedEndTime,
            activityId: defaultValues?.activityId ?? activePreset.value?.activityId ?? null,
            projectId: defaultValues?.projectId ?? activePreset.value?.projectId ?? null,
            comment: defaultValues?.comment ?? null
        };
    };

    return { createDefaultTimeEntry };
}
