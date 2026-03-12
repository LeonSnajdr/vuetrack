<template>
    <BaseDateTimeInput v-model="startTime" :label="$t('timeEntry.field.startTime')" :rules="[rules.required()]" />
</template>

<script setup lang="ts">
const startTime = defineModel<Date>({ required: true });

const rules = useRules();
</script>
