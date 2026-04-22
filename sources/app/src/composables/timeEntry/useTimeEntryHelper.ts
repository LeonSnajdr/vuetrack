import type { TimeEntryCreateContract } from "@/contracts/TimeEntryContract";
import type { Nullable } from "@/util/Nullable";
import { maxBy } from "lodash";

export function useTimeEntryHelper() {
    const presetStore = usePresetStore();
    const timeEntryStore = useTimeEntryStore();
    const { timeEntries } = storeToRefs(timeEntryStore);
    const { activePreset } = storeToRefs(presetStore);

    const hasDefaultValue = <TKey extends keyof Nullable<TimeEntryCreateContract>>(
        defaultValues: Partial<Nullable<TimeEntryCreateContract>> | undefined,
        key: TKey
    ): defaultValues is Partial<Nullable<TimeEntryCreateContract>> & Pick<Nullable<TimeEntryCreateContract>, TKey> => {
        return defaultValues !== undefined && Object.hasOwn(defaultValues, key);
    };

    const createDefaultTimeEntry = (defaultValues?: Partial<Nullable<TimeEntryCreateContract>>): Nullable<TimeEntryCreateContract> => {
        const newestTimeEntry = maxBy(timeEntries.value, (x) => x.endTime);
        const resolvedStartTime =
            hasDefaultValue(defaultValues, "startTime") ? defaultValues.startTime : newestTimeEntry?.endTime ?? new Date(new Date().setHours(8));
        const resolvedEndTime = hasDefaultValue(defaultValues, "endTime")
            ? defaultValues.endTime
            : activePreset.value?.durationMinutes != null && resolvedStartTime instanceof Date
              ? new Date(resolvedStartTime.getTime() + activePreset.value.durationMinutes * 60 * 1000)
              : null;

        return {
            taskId: hasDefaultValue(defaultValues, "taskId") ? defaultValues.taskId : activePreset.value?.taskId ?? null,
            startTime: resolvedStartTime,
            endTime: resolvedEndTime,
            activityId: hasDefaultValue(defaultValues, "activityId") ? defaultValues.activityId : activePreset.value?.activityId ?? null,
            projectId: hasDefaultValue(defaultValues, "projectId") ? defaultValues.projectId : activePreset.value?.projectId ?? null,
            comment: hasDefaultValue(defaultValues, "comment") ? defaultValues.comment : null
        };
    };

    return { createDefaultTimeEntry };
}
