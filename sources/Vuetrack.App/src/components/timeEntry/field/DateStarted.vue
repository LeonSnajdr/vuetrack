<template>
    <VRowSingle>
        <BaseDateTimeInput v-model="dateStarted" v-bind="$attrs" :label="$t('timeEntry.field.dateStarted')" :rules="[rules.required()]" />
    </VRowSingle>
</template>

<script setup lang="ts">
const dateStarted = defineModel<Date | null>({ required: true });

const rules = useRules();
</script>
