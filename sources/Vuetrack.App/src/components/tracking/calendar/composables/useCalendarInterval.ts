const availableIntervalMinutes = [5, 10, 15, 30, 60] as const;
export type CalendarInterval = (typeof availableIntervalMinutes)[number];

export function useCalendarInterval() {
    const { t } = useI18n();
    const settingsStore = useSettingsStore();
    const { calendarSettings } = storeToRefs(settingsStore);

    const intervalMinutes = computed({
        get: () => calendarSettings.value.intervalMinutes,
        set: (value) => (calendarSettings.value.intervalMinutes = value)
    });

    const firstInterval = computed(() => (calendarSettings.value.dayRange.startHour * 60) / intervalMinutes.value);
    const intervalCount = computed(() => ((calendarSettings.value.dayRange.endHour - calendarSettings.value.dayRange.startHour) * 60) / intervalMinutes.value);

    const dayRange = computed({
        get: () => [calendarSettings.value.dayRange.startHour, calendarSettings.value.dayRange.endHour],
        set: ([nextStart, nextEnd]) => {
            const clampedStart = Math.max(0, Math.min(23, nextStart));
            const clampedEnd = Math.max(clampedStart + 1, Math.min(24, nextEnd));
            calendarSettings.value.dayRange = { startHour: clampedStart, endHour: clampedEnd };
        }
    });

    const intervalMinuteOptions = computed(() =>
        availableIntervalMinutes.map((minutes) => ({
            title: t("calendar.intervalMinutes.option", { minutes }),
            value: minutes
        }))
    );

    return {
        intervalMinutes,
        firstInterval,
        intervalCount,
        dayRange,
        intervalMinuteOptions
    };
}
