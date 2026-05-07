const availableIntervalMinutes = [5, 10, 15, 30, 60] as const;
export type CalendarInterval = (typeof availableIntervalMinutes)[number];

export function useCalendarInterval() {
    const { t } = useI18n();
    const configStore = useConfigStore();
    const { calendarConfig } = storeToRefs(configStore);

    const intervalMinutes = computed({
        get: () => calendarConfig.value.intervalMinutes,
        set: (value) => (calendarConfig.value.intervalMinutes = value)
    });

    const intervalCount = computed(() => (60 * 24) / intervalMinutes.value);
    const intervalMinuteOptions = computed(() =>
        availableIntervalMinutes.map((minutes) => ({
            title: t("calendar.intervalMinutes.option", { minutes }),
            value: minutes
        }))
    );

    return {
        intervalMinutes,
        intervalCount,
        intervalMinuteOptions
    };
}
