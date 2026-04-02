<template>
    <div class="d-flex align-center ga-1">
        <VIcon :icon="mdiCalendarRange" class="opacity-70" size="small" />
        <p>{{ periodLabel }}</p>
        <VIcon :icon="mdiMenuDown" class="opacity-70" size="small" />
        <VMenu v-model="menuOpen" :closeOnContentClick="false" activator="parent" location="bottom start">
            <VCard minWidth="500">
                <VCardText class="d-flex">
                    <div class="d-flex flex-column ga-2 mr-4">
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
                    >
                        <template #day="{ props, item }">
                            <VBtn @click="onPickerDayClick(item.date, props.onClick)" v-bind="getDayButtonProps(props)">
                                {{ item.localized }}
                            </VBtn>
                        </template>
                    </VDatePicker>
                </VCardText>
            </VCard>
        </VMenu>
    </div>
</template>

<script setup lang="ts">
import type { TrackingPeriodPreset } from "@/composables/tracking/useTrackingTimePeriod";

const { t, locale } = useI18n();
const {
    from,
    to,
    setPreset: applyPreset,
    applyPeriod,
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

const syncPickerRange = () => {
    pickerRange.value = [startOfDay(from.value), startOfDay(to.value)];
};

const setPreset = (preset: Exclude<TrackingPeriodPreset, "custom">) => {
    applyPreset(preset);
    menuOpen.value = false;
};

watch(
    [from, to],
    () => {
        syncPickerRange();
    },
    { immediate: true }
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

const onPickerDayClick = (value: unknown, defaultClick?: () => void) => {
    const nextDay = value instanceof Date && !Number.isNaN(value.getTime()) ? startOfDay(value) : null;
    const repeatedSingleDayClick = nextDay !== null && pickerRange.value.length === 1 && sameDay(pickerRange.value[0], nextDay);

    defaultClick?.();

    if (!repeatedSingleDayClick || nextDay === null) return;

    pickerRange.value = [nextDay, nextDay];
    applyPeriod(nextDay, nextDay);
    menuOpen.value = false;
};

const getDayButtonProps = (props: Record<string, unknown>) => {
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    const { onClick: _onClick, ...buttonProps } = props;

    return buttonProps;
};

const detectedPreset = computed<TrackingPeriodPreset>(() => {
    const today = new Date();
    const normalizedStart = startOfDay(from.value);
    const normalizedEnd = endOfDay(to.value);

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
    const rangeLabel = sameDay(from.value, to.value)
        ? dateFormatter.value.format(from.value)
        : `${dateFormatter.value.format(from.value)} - ${dateFormatter.value.format(to.value)}`;

    if (detectedPreset.value === "custom") return rangeLabel;

    return presetLabels[detectedPreset.value];
});

const getPresetVariant = computed(() => (preset: Exclude<TrackingPeriodPreset, "custom">) => {
    return detectedPreset.value === preset ? "tonal" : "flat";
});
</script>
