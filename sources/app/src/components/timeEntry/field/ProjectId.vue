<template>
    <VRowSingle>
        <VNumberInput v-bind="$attrs" v-model="projectId" :label="$t('timeEntry.field.projectId')" :rules="[rules.required()]" />
    </VRowSingle>
</template>

<script setup lang="ts">
import type { ProjectId } from "@/contracts/TimeEntryContract";

const projectId = defineModel<ProjectId>({ required: true });

const rules = useRules();
</script>
