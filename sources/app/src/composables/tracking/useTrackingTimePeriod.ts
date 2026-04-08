export type TrackingPeriodPreset = "custom" | "last7Days" | "lastMonth" | "thisMonth" | "today" | "workweek" | "yesterday";

export function useTrackingTimePeriod() {
    const { updateFilter } = useTrackingFilter();
    const { startOfDay, endOfDay, startOfWeek, endOfWorkWeek, addDays, startOfMonth, endOfMonth, sameDay, normalizeRange } = useDateHelper();

    const applyPeriod = (start: Date, end: Date) => {
        const { start: nextStart, end: nextEnd } = normalizeRange(start, end);

        void updateFilter({
            from: startOfDay(nextStart),
            to: endOfDay(nextEnd)
        });
    };

    const jumpToDay = (date: Date) => {
        applyPeriod(date, date);
    };

    const setPreset = (preset: Exclude<TrackingPeriodPreset, "custom">) => {
        const today = new Date();
        let start: Date;
        let end: Date;

        if (preset === "today") {
            start = today;
            end = today;
        } else if (preset === "yesterday") {
            const yesterday = addDays(today, -1);
            start = yesterday;
            end = yesterday;
        } else if (preset === "last7Days") {
            start = addDays(today, -6);
            end = today;
        } else if (preset === "workweek") {
            start = startOfWeek(today, 1);
            end = endOfWorkWeek(today, 1);
        } else if (preset === "thisMonth") {
            start = startOfMonth(today);
            end = endOfMonth(today);
        } else {
            const lastMonth = new Date(today.getFullYear(), today.getMonth() - 1, 1);
            start = startOfMonth(lastMonth);
            end = endOfMonth(lastMonth);
        }

        applyPeriod(start, end);
    };

    return {
        setPreset,
        jumpToDay,
        applyPeriod,
        startOfDay,
        endOfDay,
        startOfWeek,
        endOfWorkWeek,
        addDays,
        startOfMonth,
        endOfMonth,
        sameDay
    };
}
