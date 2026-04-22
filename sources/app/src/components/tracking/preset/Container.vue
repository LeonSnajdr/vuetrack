<template>
    <VRowSingle>
        <VTextField v-model.trim="preset.name" :label="$t('sidebar.preset.field.name')" maxlength="20" autofocus />
    </VRowSingle>
    <TimeEntryFieldTaskId v-model="preset.taskId" :required="false" clearable />
    <VRowSingle>
        <VNumberInput v-model="preset.durationMinutes" :label="$t('sidebar.preset.field.durationMinutes')" :min="1" controlVariant="default" clearable />
    </VRowSingle>
    <TimeEntryFieldProjectId v-model="preset.projectId" :autoSelectFirst="false" :inferFromTaskId="false" :required="false" :taskId="preset.taskId" clearable />
    <TimeEntryFieldActivityId v-model="preset.activityId" :autoSelectFirst="false" :projectId="preset.projectId" :required="false" clearable />
</template>

<script setup lang="ts">
import type { TimeEntryPresetCreate } from "@/models/TimeEntryPreset";

const preset = defineModel<TimeEntryPresetCreate>({ required: true });
</script>
