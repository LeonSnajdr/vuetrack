<template>
    <VRowSingle>
        <VTextField v-bind="$attrs" v-model.trim="taskId" :label="$t('timeEntry.field.taskId')" :rules="disableRequired ? undefined : [rules.required()]" />
    </VRowSingle>
</template>

<script setup lang="ts">
defineProps<{
    disableRequired?: boolean;
}>();

const taskId = defineModel<string | null>({ required: true });

const rules = useRules();
</script>
