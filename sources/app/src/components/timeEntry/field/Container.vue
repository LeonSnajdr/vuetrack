<template>
    <VForm :key="reFocusKey" v-model="valid">
        <TimeEntryFieldTaskId v-model="timeEntry.taskId" :autofocus="firstEmptyField === 'taskId'" :errorMessages="errorMessages.taskId" />
        <VRow :class="isEndTimeFullMode ? 'ga-4' : 'ga-2'">
            <VCol :cols="isEndTimeFullMode ? undefined : 8">
                <TimeEntryFieldDateStarted
                    v-model="timeEntry.dateStarted"
                    :autofocus="firstEmptyField === 'dateStarted'"
                    :errorMessages="errorMessages.dateStarted"
                    :tabindex="skipTimeFields ? -1 : undefined"
                />
            </VCol>
            <VCol :cols="isEndTimeFullMode ? 12 : undefined">
                <TimeEntryFieldDateEnded
                    v-model="timeEntry.dateEnded"
                    v-model:fullMode="isEndTimeFullMode"
                    :autofocus="firstEmptyField === 'dateEnded'"
                    :dateStarted="timeEntry.dateStarted"
                    :errorMessages="errorMessages.dateEnded"
                    :tabindex="skipTimeFields ? -1 : undefined"
                />
            </VCol>
        </VRow>
        <TimeEntryFieldProjectId
            v-model="timeEntry.projectId"
            :autofocus="firstEmptyField === 'projectId'"
            :errorMessages="errorMessages.projectId"
            :taskId="timeEntry.taskId"
        />
        <TimeEntryFieldActivityId
            v-model="timeEntry.activityId"
            :autofocus="firstEmptyField === 'activityId'"
            :errorMessages="errorMessages.activityId"
            :projectId="timeEntry.projectId"
        />
        <TimeEntryFieldComment v-model="timeEntry.comment" :autofocus="firstEmptyField === 'comment'" :errorMessages="errorMessages.comment" />
    </VForm>
</template>

<script setup lang="ts">
import type { ActivityId } from "@/contracts/ActivityContract";
import type { ProjectId } from "@/contracts/ProjectContract";
import type { ValidationErrors } from "@/util/ValidationProblem";

const props = defineProps<{
    skipTimeFields?: boolean;
}>();

const timeEntry = defineModel<{
    taskId: string | null;
    dateStarted: Date | null;
    dateEnded: Date | null;
    projectId: ProjectId | null;
    activityId: ActivityId | null;
    comment: string | null;
}>({ required: true });

const valid = defineModel<boolean>("valid", { default: false });
const fieldErrors = defineModel<ValidationErrors | undefined>("errors");
const { messages: errorMessages } = useValidation(fieldErrors);
const isEndTimeFullMode = ref(false);

const firstEmptyField = computed(() => {
    if (!timeEntry.value.taskId) return "taskId";
    if (!props.skipTimeFields && !timeEntry.value.dateStarted) return "dateStarted";
    if (!props.skipTimeFields && !timeEntry.value.dateEnded) return "dateEnded";
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
