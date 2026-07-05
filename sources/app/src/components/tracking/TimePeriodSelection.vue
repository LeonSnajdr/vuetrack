<template>
    <div class="d-flex align-center ga-1">
        <VIconBtn
            @click="shiftPeriod(detectedPreset, filter.from, filter.to, -1)"
            :icon="mdiChevronLeft"
            :title="t('tracking.period.previous')"
            variant="text"
        />
        <div class="d-flex align-center justify-center ga-1" style="cursor: pointer; width: 130px">
            {{ periodLabel }}
            <VMenu v-model="menuOpen" :closeOnContentClick="false" activator="parent" location="bottom start">
                <VCard minWidth="800">
                    <VCardText>
                        <VRow>
                            <VCol cols="3">
                                <VList nav>
                                    <VListItem
                                        v-for="preset in presets"
                                        :key="preset.value"
                                        @click="setPreset(preset.value)"
                                        :active="detectedPreset === preset.value"
                                        :title="preset.title"
                                    />
                                </VList>
                            </VCol>
                            <VCol cols="9">
                                <VDateRangePicker
                                    @update:modelValue="onPickerRangeUpdate"
                                    :modelValue="pickerRange"
                                    class="w-100 mt-n2"
                                    hideHeader
                                    showAdjacentMonths
                                />
                            </VCol>
                        </VRow>
                    </VCardText>
                </VCard>
            </VMenu>
        </div>
        <VIconBtn @click="shiftPeriod(detectedPreset, filter.from, filter.to, 1)" :icon="mdiChevronRight" :title="t('tracking.period.next')" variant="text" />
    </div>
</template>

<script setup lang="ts">
import type { TrackingPeriodPreset } from "@/composables/tracking/useTrackingTimePeriod";

const { t, locale } = useI18n();
const { filter } = useTrackingFilter();
const {
    setPreset: applyPreset,
    applyPeriod,
    shiftPeriod,
    startOfDay,
    endOfDay,
    startOfWeek,
    endOfWorkWeek,
    addDays,
    startOfMonth,
    endOfMonth,
    sameDay
} = useTrackingTimePeriod();

const dateFormatter = computed(() => new Intl.DateTimeFormat(locale.value, { month: "short", day: "numeric" }));
const menuOpen = ref(false);
const pickerRange = ref<Date[]>([]);

const presets = computed<{ title: string; value: Exclude<TrackingPeriodPreset, "custom"> }[]>(() => [
    { title: t("tracking.period.today"), value: "today" },
    { title: t("tracking.period.yesterday"), value: "yesterday" },
    { title: t("tracking.period.last7Days"), value: "last7Days" },
    { title: t("tracking.period.workweek"), value: "workweek" },
    { title: t("tracking.period.thisMonth"), value: "thisMonth" },
    { title: t("tracking.period.lastMonth"), value: "lastMonth" }
]);

const syncPickerRange = () => {
    pickerRange.value = [startOfDay(filter.value.from), startOfDay(filter.value.to)];
};

const setPreset = (preset: Exclude<TrackingPeriodPreset, "custom">) => {
    applyPreset(preset);
    menuOpen.value = false;
};

watch(
    filter,
    () => {
        syncPickerRange();
    },
    { deep: true, immediate: true }
);

watch(menuOpen, (isOpen) => {
    if (isOpen) {
        syncPickerRange();
        return;
    }

    if (pickerRange.value.length !== 1) return;

    applyPeriod(pickerRange.value[0], pickerRange.value[0]);
});

const onPickerRangeUpdate = (value: unknown) => {
    const nextRange = Array.isArray(value) ? value.filter((item): item is Date => item instanceof Date && !Number.isNaN(item.getTime())) : [];
    pickerRange.value = nextRange;

    if (nextRange.length < 2) return;

    applyPeriod(nextRange[0], nextRange[nextRange.length - 1]);
    menuOpen.value = false;
};

const detectedPreset = computed<TrackingPeriodPreset>(() => {
    const today = new Date();
    const normalizedStart = startOfDay(filter.value.from);
    const normalizedEnd = endOfDay(filter.value.to);

    const yesterday = addDays(today, -1);

    if (sameDay(normalizedStart, today) && sameDay(normalizedEnd, today)) return "today";
    if (sameDay(normalizedStart, yesterday) && sameDay(normalizedEnd, yesterday)) return "yesterday";
    if (sameDay(normalizedStart, addDays(today, -6)) && sameDay(normalizedEnd, today)) return "last7Days";
    if (normalizedStart.getTime() === startOfWeek(today, 1).getTime() && normalizedEnd.getTime() === endOfWorkWeek(today, 1).getTime()) return "workweek";
    if (normalizedStart.getTime() === startOfMonth(today).getTime() && normalizedEnd.getTime() === endOfMonth(today).getTime()) return "thisMonth";

    const lastMonth = new Date(today.getFullYear(), today.getMonth() - 1, 1);
    if (normalizedStart.getTime() === startOfMonth(lastMonth).getTime() && normalizedEnd.getTime() === endOfMonth(lastMonth).getTime()) return "lastMonth";

    return "custom";
});

const periodLabel = computed(() => {
    const rangeLabel = sameDay(filter.value.from, filter.value.to)
        ? dateFormatter.value.format(filter.value.from)
        : `${dateFormatter.value.format(filter.value.from)} - ${dateFormatter.value.format(filter.value.to)}`;

    if (detectedPreset.value === "custom") return rangeLabel;

    return presets.value.find((preset) => preset.value === detectedPreset.value)?.title ?? rangeLabel;
});
</script>
