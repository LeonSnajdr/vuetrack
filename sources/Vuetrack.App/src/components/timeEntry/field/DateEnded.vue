<template>
    <VRowSingle>
        <BaseDateTimeInput
            v-model="dateEnded"
            v-model:fullMode="fullMode"
            v-bind="$attrs"
            :disabled="!dateStarted"
            :label="$t('timeEntry.field.dateEnded')"
            :min="dateStarted"
            :referenceDate="dateStarted"
            :rules="granularEndTimeRules"
            mode="time"
        />
    </VRowSingle>
</template>

<script setup lang="ts">
const props = defineProps<{
    dateStarted: Date | null;
}>();

const dateEnded = defineModel<Date | null>({ required: true });
const fullMode = defineModel<boolean>("fullMode", { default: false });

const rules = useRules();

const granularEndTimeRules = computed(() => [rules.required(), ...(props.dateStarted ? [rules.dateAfter(props.dateStarted)] : [])]);
</script>
