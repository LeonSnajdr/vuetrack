<template>
    <VForm v-model="valid">
        <TimeEntryFieldTaskId v-model="timeEntry.taskId" autofocus />
        <TimeEntryFieldStartTime v-model="timeEntry.startTime" :tabindex="skipTimeFields ? -1 : undefined" />
        <TimeEntryFieldEndTime v-model="timeEntry.endTime" :startTime="timeEntry.startTime" :tabindex="skipTimeFields ? -1 : undefined" />
        <TimeEntryFieldProjectId v-model="timeEntry.projectId" :taskId="timeEntry.taskId" />
        <TimeEntryFieldActivityId v-model="timeEntry.activityId" :projectId="timeEntry.projectId" />
        <TimeEntryFieldComment v-model="timeEntry.comment" />
    </VForm>
</template>

<script setup lang="ts">
import type { ActivityId } from "@/contracts/ActivityContract";
import type { ProjectId } from "@/contracts/ProjectContract";

defineProps<{
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
</script>
