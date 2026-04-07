export function useCalendarTimePeriod() {
    const { filter } = useTrackingFilter();

    const start = computed(() => {
        const nextDate = new Date(filter.value.from);
        nextDate.setHours(0, 0, 0, 0);
        return nextDate;
    });

    const end = computed(() => {
        const nextDate = new Date(filter.value.to);
        nextDate.setHours(23, 59, 59, 999);
        return nextDate;
    });

    const weekdays = computed(() => {
        const includedDays = new Set<number>();
        const cursor = new Date(start.value);

        while (cursor <= end.value && includedDays.size < 7) {
            includedDays.add(cursor.getDay());
            cursor.setDate(cursor.getDate() + 1);
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
