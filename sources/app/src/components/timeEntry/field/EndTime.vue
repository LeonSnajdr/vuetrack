<template>
    <VRowSingle>
        <BaseDateTimeInput ref="endTimeInputRef" v-model="endTime" :label="$t('timeEntry.field.endTime')" :rules="validationRules" />
    </VRowSingle>
</template>

<script setup lang="ts">
const props = defineProps<{
    startTime: Date | null | undefined;
}>();

const endTime = defineModel<Date>({ required: true });

const { t } = useI18n();
const rules = useRules();
const endTimeInputRef = useTemplateRef("endTimeInputRef");
const validationRules = computed(() => [
    rules.required(),
    rules.dateAfter(props.startTime, t("timeEntry.field.endTime.dateAfter"))
]);

watch(
    () => props.startTime?.getTime() ?? null,
    () => {
        void endTimeInputRef.value?.validate?.();
    }
);
</script>
