<template>
    <VForm :key="reFocusKey" v-model="valid">
        <TimeEntryFieldTaskId v-model="timeEntry.taskId" :autofocus="firstEmptyField === 'taskId'" />
        <TimeEntryFieldStartTime v-model="timeEntry.startTime" :autofocus="firstEmptyField === 'startTime'" :tabindex="skipTimeFields ? -1 : undefined" />
        <TimeEntryFieldEndTime
            v-model="timeEntry.endTime"
            :autofocus="firstEmptyField === 'endTime'"
            :startTime="timeEntry.startTime"
            :tabindex="skipTimeFields ? -1 : undefined"
        />
        <TimeEntryFieldProjectId v-model="timeEntry.projectId" :autofocus="firstEmptyField === 'projectId'" :taskId="timeEntry.taskId" />
        <TimeEntryFieldActivityId v-model="timeEntry.activityId" :autofocus="firstEmptyField === 'activityId'" :projectId="timeEntry.projectId" />
        <TimeEntryFieldComment v-model="timeEntry.comment" :autofocus="firstEmptyField === 'comment'" />
    </VForm>
</template>

<script setup lang="ts">
import type { ActivityId } from "@/contracts/ActivityContract";
import type { ProjectId } from "@/contracts/ProjectContract";

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
</script>
