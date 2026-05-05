<template>
    <VForm :key="reFocusKey" v-model="valid">
        <TimeEntryFieldTaskId v-model="timeEntry.taskId" :autofocus="firstEmptyField === 'taskId'" :errorMessages="fieldErrors?.taskId" />
        <VRow :class="isEndTimeFullMode ? 'ga-4' : 'ga-2'">
            <VCol :cols="isEndTimeFullMode ? undefined : 8">
                <TimeEntryFieldStartTime
                    v-model="timeEntry.startTime"
                    :autofocus="firstEmptyField === 'startTime'"
                    :errorMessages="fieldErrors?.startTime"
                    :tabindex="skipTimeFields ? -1 : undefined"
                />
            </VCol>
            <VCol :cols="isEndTimeFullMode ? 12 : undefined">
                <TimeEntryFieldEndTime
                    v-model="timeEntry.endTime"
                    v-model:fullMode="isEndTimeFullMode"
                    :autofocus="firstEmptyField === 'endTime'"
                    :errorMessages="fieldErrors?.endTime"
                    :startTime="timeEntry.startTime"
                    :tabindex="skipTimeFields ? -1 : undefined"
                />
            </VCol>
        </VRow>
        <TimeEntryFieldProjectId
            v-model="timeEntry.projectId"
            :autofocus="firstEmptyField === 'projectId'"
            :errorMessages="fieldErrors?.projectId"
            :taskId="timeEntry.taskId"
        />
        <TimeEntryFieldActivityId
            v-model="timeEntry.activityId"
            :autofocus="firstEmptyField === 'activityId'"
            :errorMessages="fieldErrors?.activityId"
            :projectId="timeEntry.projectId"
        />
        <TimeEntryFieldComment v-model="timeEntry.comment" :autofocus="firstEmptyField === 'comment'" :errorMessages="fieldErrors?.comment" />
    </VForm>
</template>

<script setup lang="ts">
import type { ActivityId } from "@/contracts/ActivityContract";
import type { ProjectId } from "@/contracts/ProjectContract";
import type { ApiValidationError } from "@/util/ApiValidationError";

const props = defineProps<{
    skipTimeFields?: boolean;
}>();

const timeEntry = defineModel<{
    taskId: string | null;
    startTime: Date | null;
    endTime: Date | null;
    projectId: ProjectId | null;
    activityId: ActivityId | null;
    comment: string | null;
}>({ required: true });

const valid = defineModel<boolean>("valid", { default: false });
const fieldErrors = defineModel<ApiValidationError | undefined>("errors");
const isEndTimeFullMode = ref(false);

const firstEmptyField = computed(() => {
    if (!timeEntry.value.taskId) return "taskId";
    if (!props.skipTimeFields && !timeEntry.value.startTime) return "startTime";
    if (!props.skipTimeFields && !timeEntry.value.endTime) return "endTime";
    if (!timeEntry.value.projectId) return "projectId";
    if (!timeEntry.value.activityId) return "activityId";
    if (!timeEntry.value.comment) return "comment";

    return null;
});

// If the time entry is completely reset (which could happen trough create another), the form is rerenderd to focus again the correct field
const reFocusKey = ref(crypto.randomUUID());
watch(timeEntry, () => (reFocusKey.value = crypto.randomUUID()));

watch(
    timeEntry,
    () => {
        if (!fieldErrors.value) return;
        fieldErrors.value = undefined;
    },
    { deep: true }
);
</script>
