<template>
    <VForm v-model="valid">
        <TimeEntryFieldTaskId v-model="timeEntry.taskId" autofocus />
        <TimeEntryFieldStartTime v-model="timeEntry.startTime" />
        <TimeEntryFieldEndTime v-model="timeEntry.endTime" :startTime="timeEntry.startTime" />
        <TimeEntryFieldProjectId v-model="timeEntry.projectId" :taskId="timeEntry.taskId" />
        <TimeEntryFieldActivityId v-model="timeEntry.activityId" :projectId="timeEntry.projectId" />
        <TimeEntryFieldComment v-model="timeEntry.comment" />
    </VForm>
</template>

<script setup lang="ts">
import type { ActivityId } from "@/contracts/ActivityContract";
import type { ProjectId } from "@/contracts/ProjectContract";

const timeEntry = defineModel<{
    taskId: string;
    startTime: Date;
    endTime: Date;
    projectId: ProjectId;
    activityId: ActivityId;
    comment: string;
}>({ required: true });

const valid = defineModel<boolean>("valid", { default: false });
</script>
