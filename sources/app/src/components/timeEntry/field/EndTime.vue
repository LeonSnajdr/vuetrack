<template>
    <VRowSingle>
        <BaseDateTimeInput v-model="endTime" :label="$t('timeEntry.field.endTime')" :rules="endTimeRules" />
    </VRowSingle>
</template>

<script setup lang="ts">
const props = defineProps<{
    startTime: Date | null;
}>();

const endTime = defineModel<Date | null>({ required: true });

const rules = useRules();
const endTimeRules = computed(() => [rules.required(), ...(props.startTime ? [rules.dateAfter(props.startTime)] : [])]);
</script>
