<template>
    <VRowSingle>
        <VTextField v-bind="$attrs" v-model.trim="taskId" :label="$t('timeEntry.field.taskId')" :rules="[rules.required()]" />
    </VRowSingle>
</template>

<script setup lang="ts">
const taskId = defineModel<string>({ required: true });

const rules = useRules();
</script>
