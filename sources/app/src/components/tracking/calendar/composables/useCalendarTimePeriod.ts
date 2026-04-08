export function useCalendarTimePeriod() {
    const { filter } = useTrackingFilter();
    const { startOfDay, endOfDay, addDays } = useDateHelper();

    const start = computed(() => startOfDay(filter.value.from));

    const end = computed(() => endOfDay(filter.value.to));

    const weekdays = computed(() => {
        const includedDays = new Set<number>();
        const cursor = new Date(start.value);

        while (cursor <= end.value && includedDays.size < 7) {
            includedDays.add(cursor.getDay());
            cursor.setTime(addDays(cursor, 1).getTime());
        }

        return [1, 2, 3, 4, 5, 6, 0].filter((weekday) => includedDays.has(weekday));
    });

    const dayCount = computed(() => {
        const millisecondsPerDay = 24 * 60 * 60 * 1000;
        return Math.floor((end.value.getTime() - start.value.getTime()) / millisecondsPerDay) + 1;
    });

    const isReadonly = computed(() => dayCount.value > 7);
    const calendarType = computed<"month" | "week">(() => (isReadonly.value ? "month" : "week"));

    return { start, end, weekdays, isReadonly, calendarType };
}
