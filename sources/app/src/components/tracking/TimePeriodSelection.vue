<template>
    <VBtn :appendIcon="mdiChevronDown" :prependIcon="mdiCalendarRange">
        {{ periodLabel }}
        <VMenu v-model="menuOpen" :closeOnContentClick="false" activator="parent" location="bottom start">
            <VCard minWidth="500">
                <VCardText class="d-flex ga-2">
                    <div class="d-flex flex-column ga-2">
                        <VBtn @click="setPreset('today')" :variant="getPresetVariant('today')" justify="start">{{ $t("tracking.period.today") }}</VBtn>
                        <VBtn @click="setPreset('yesterday')" :variant="getPresetVariant('yesterday')" justify="start">
                            {{ $t("tracking.period.yesterday") }}
                        </VBtn>
                        <VBtn @click="setPreset('last7Days')" :variant="getPresetVariant('last7Days')" justify="start">
                            {{ $t("tracking.period.last7Days") }}
                        </VBtn>
                        <VBtn @click="setPreset('workweek')" :variant="getPresetVariant('workweek')" justify="start">
                            {{ $t("tracking.period.workweek") }}
                        </VBtn>
                        <VBtn @click="setPreset('thisMonth')" :variant="getPresetVariant('thisMonth')" justify="start">
                            {{ $t("tracking.period.thisMonth") }}
                        </VBtn>
                        <VBtn @click="setPreset('lastMonth')" :variant="getPresetVariant('lastMonth')" justify="start">
                            {{ $t("tracking.period.lastMonth") }}
                        </VBtn>
                    </div>
                    <VDivider vertical />
                    <VDatePicker
                        @update:modelValue="onPickerRangeUpdate"
                        :header="$t('tracking.period.customRange')"
                        :modelValue="pickerRange"
                        class="w-100 mt-n2"
                        multiple="range"
                        hideHeader
                        showAdjacentMonths
                    />
                </VCardText>
            </VCard>
        </VMenu>
    </VBtn>
</template>

<script setup lang="ts">
type TrackingPeriodPreset = "custom" | "last7Days" | "lastMonth" | "thisMonth" | "today" | "workweek" | "yesterday";

const startTime = defineModel<Date>("startTime", { required: true });
const endTime = defineModel<Date>("endTime", { required: true });

const { t, locale } = useI18n();

const dateFormatter = computed(() => new Intl.DateTimeFormat(locale.value, { month: "short", day: "numeric" }));
const menuOpen = ref(false);
const pickerRange = ref<Date[]>([]);

const startOfDay = (date: Date) => {
    const nextDate = new Date(date);
    nextDate.setHours(0, 0, 0, 0);
    return nextDate;
};

const endOfDay = (date: Date) => {
    const nextDate = new Date(date);
    nextDate.setHours(23, 59, 59, 999);
    return nextDate;
};

const startOfWeek = (date: Date, weekStartsOn: 0 | 1 = 1) => {
    const nextDate = startOfDay(date);
    const diff = (nextDate.getDay() - weekStartsOn + 7) % 7;
    nextDate.setDate(nextDate.getDate() - diff);
    return nextDate;
};

const endOfWorkWeek = (date: Date, weekStartsOn: 0 | 1 = 1) => {
    const nextDate = startOfWeek(date, weekStartsOn);
    nextDate.setDate(nextDate.getDate() + 4);
    nextDate.setHours(23, 59, 59, 999);
    return nextDate;
};

const addDays = (date: Date, amount: number) => {
    const nextDate = new Date(date);
    nextDate.setDate(nextDate.getDate() + amount);
    return nextDate;
};

const startOfMonth = (date: Date) => {
    const nextDate = startOfDay(date);
    nextDate.setDate(1);
    return nextDate;
};

const endOfMonth = (date: Date) => {
    const nextDate = startOfDay(date);
    nextDate.setMonth(nextDate.getMonth() + 1, 0);
    nextDate.setHours(23, 59, 59, 999);
    return nextDate;
};

const sameDay = (left: Date, right: Date) => startOfDay(left).getTime() === startOfDay(right).getTime();

const applyPeriod = (start: Date, end: Date) => {
    const nextStart = start <= end ? start : end;
    const nextEnd = start <= end ? end : start;

    startTime.value = startOfDay(nextStart);
    endTime.value = endOfDay(nextEnd);
};

const setPreset = (preset: Exclude<TrackingPeriodPreset, "custom">) => {
    const today = new Date();

    if (preset === "today") {
        applyPeriod(today, today);
        return;
    }

    if (preset === "yesterday") {
        const yesterday = addDays(today, -1);
        applyPeriod(yesterday, yesterday);
        return;
    }

    if (preset === "last7Days") {
        applyPeriod(addDays(today, -6), today);
        return;
    }

    if (preset === "workweek") {
        applyPeriod(startOfWeek(today, 1), endOfWorkWeek(today, 1));
        return;
    }

    if (preset === "thisMonth") {
        applyPeriod(startOfMonth(today), endOfMonth(today));
        return;
    }

    const lastMonth = new Date(today.getFullYear(), today.getMonth() - 1, 1);
    applyPeriod(startOfMonth(lastMonth), endOfMonth(lastMonth));
};

watch(
    [startTime, endTime],
    () => {
        pickerRange.value = [startOfDay(startTime.value), startOfDay(endTime.value)];
    },
    { immediate: true }
);

watch(menuOpen, (isOpen) => {
    if (isOpen || pickerRange.value.length !== 1) return;

    applyPeriod(pickerRange.value[0], pickerRange.value[0]);
});

const onPickerRangeUpdate = (value: unknown) => {
    const nextRange = Array.isArray(value) ? value.filter((item): item is Date => item instanceof Date && !Number.isNaN(item.getTime())) : [];
    pickerRange.value = nextRange;

    if (nextRange.length < 2) return;

    applyPeriod(nextRange[0], nextRange[nextRange.length - 1]);
};

const detectedPreset = computed<TrackingPeriodPreset>(() => {
    const today = new Date();
    const normalizedStart = startOfDay(startTime.value);
    const normalizedEnd = endOfDay(endTime.value);

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
    const presetLabels: Record<Exclude<TrackingPeriodPreset, "custom">, string> = {
        today: t("tracking.period.today"),
        yesterday: t("tracking.period.yesterday"),
        last7Days: t("tracking.period.last7Days"),
        workweek: t("tracking.period.workweek"),
        thisMonth: t("tracking.period.thisMonth"),
        lastMonth: t("tracking.period.lastMonth")
    };
    const rangeLabel = `${dateFormatter.value.format(startTime.value)} - ${dateFormatter.value.format(endTime.value)}`;

    if (detectedPreset.value === "custom") return rangeLabel;

    return `${presetLabels[detectedPreset.value]} · ${rangeLabel}`;
});

const getPresetVariant = (preset: Exclude<TrackingPeriodPreset, "custom">) => {
    return detectedPreset.value === preset ? "flat" : "tonal";
};
</script>
