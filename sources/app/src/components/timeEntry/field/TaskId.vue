<template>
    <VRowSingle>
        <VTextField v-bind="$attrs" v-model.trim="taskId" :label="$t('timeEntry.field.taskId')" :rules="required ? [rules.required()] : undefined" />
    </VRowSingle>
</template>

<script setup lang="ts">
const { required = true } = defineProps<{
    required?: boolean;
}>();

const taskId = defineModel<string | null>({ required: true });

const rules = useRules();
</script>
