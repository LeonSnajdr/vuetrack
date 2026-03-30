<template>
    <VRowSingle>
        <BaseDateTimeInput v-model="startTime" :label="$t('timeEntry.field.startTime')" :rules="[rules.required()]" />
    </VRowSingle>
</template>

<script setup lang="ts">
const startTime = defineModel<Date>({ required: true });

const rules = useRules();
</script>
