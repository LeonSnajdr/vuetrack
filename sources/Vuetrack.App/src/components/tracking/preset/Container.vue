<template>
    <VRowSingle>
        <VTextField v-model.trim="preset.name" :label="$t('preset.field.name')" :rules="[rules.required()]" maxlength="20" autofocus />
    </VRowSingle>
    <TimeEntryFieldTaskId v-model="preset.taskId" :required="false" clearable />
    <VRowSingle>
        <VNumberInput v-model="preset.durationMinutes" :label="$t('preset.field.durationMinutes')" :min="1" controlVariant="default" clearable />
    </VRowSingle>
    <TimeEntryFieldProjectId v-model="preset.projectId" :taskId="preset.taskId" clearable disableAutoSelectFirst disableInferFromTaskId disableRequired />
    <TimeEntryFieldActivityId v-model="preset.activityId" :projectId="preset.projectId" clearable disableAutoSelectFirst disableRequired />
</template>

<script setup lang="ts">
const preset = defineModel<{
    name: string | null;
    taskId: string | null;
    durationMinutes: number | null;
    projectId: ProjectId | null;
    activityId: ActivityId | null;
}>({ required: true });

const rules = useRules();
</script>
