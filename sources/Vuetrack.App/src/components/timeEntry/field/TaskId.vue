<template>
    <VRowSingle>
        <VTextField v-bind="$attrs" v-model.trim="taskId" :label="$t('timeEntry.field.taskId')" />
    </VRowSingle>
</template>

<script setup lang="ts">
const taskId = defineModel<string | null>({ required: true });
</script>
