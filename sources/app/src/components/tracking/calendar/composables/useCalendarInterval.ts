export function useCalendarInterval() {
    const { t } = useI18n();
    const calendarStore = useCalendarStore();
    const { intervalMinutes } = storeToRefs(calendarStore);

    const availableIntervalMinutes = [5, 10, 15, 30, 60];

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
