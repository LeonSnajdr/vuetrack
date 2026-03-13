export function useCalendarTimePeriod() {
    const trackingStore = useTrackingStore();
    const { startTime, endTime } = storeToRefs(trackingStore);

    const start = computed(() => {
        const nextDate = new Date(startTime.value);
        nextDate.setHours(0, 0, 0, 0);
        return nextDate;
    });

    const end = computed(() => {
        const nextDate = new Date(endTime.value);
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

    return { start, end, weekdays };
}
