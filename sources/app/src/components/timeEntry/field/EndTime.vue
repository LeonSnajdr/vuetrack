<template>
    <VRowSingle>
        <BaseDateTimeInput
            ref="endTimeInputRef"
            v-model="endTime"
            :label="$t('timeEntry.field.endTime')"
            :rules="[rules.required(), rules.dateAfter(props.startTime)]"
        />
    </VRowSingle>
</template>

<script setup lang="ts">
const props = defineProps<{
    startTime: Date;
}>();

const endTime = defineModel<Date>({ required: true });

const rules = useRules();
</script>
