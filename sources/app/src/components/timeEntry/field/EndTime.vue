<template>
    <VRowSingle>
        <BaseDateTimeInput
            v-model="endTime"
            v-model:fullMode="fullMode"
            v-bind="$attrs"
            :disabled="!startTime"
            :label="$t('timeEntry.field.endTime')"
            :min="startTime"
            :referenceDate="startTime"
            :rules="granularEndTimeRules"
            mode="time"
        />
    </VRowSingle>
</template>

<script setup lang="ts">
const props = defineProps<{
    startTime: Date | null;
}>();

const endTime = defineModel<Date | null>({ required: true });
const fullMode = defineModel<boolean>("fullMode", { default: false });

const rules = useRules();

const granularEndTimeRules = computed(() => [rules.required(), ...(props.startTime ? [rules.dateAfter(props.startTime)] : [])]);
</script>
