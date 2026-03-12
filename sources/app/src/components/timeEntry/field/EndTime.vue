<template>
    <BaseDateTimeInput v-model="endTime" :label="$t('timeEntry.field.endTime')" :rules="[rules.required()]" />
</template>

<script setup lang="ts">
const endTime = defineModel<Date>({ required: true });

const rules = useRules();
</script>
