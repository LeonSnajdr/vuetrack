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

    const shiftPeriod = (from: Date, to: Date, direction: 1 | -1) => {
        const normalizedStart = startOfDay(from);
        const normalizedEnd = endOfDay(to);

        // Full calendar month → step by month
        if (normalizedStart.getTime() === startOfMonth(from).getTime() && normalizedEnd.getTime() === endOfMonth(from).getTime()) {
            const anchor = new Date(from.getFullYear(), from.getMonth() + direction, 1);
            applyPeriod(startOfMonth(anchor), endOfMonth(anchor));
            return;
        }

        // Work week (Mon–Fri of one week) → step by 7 days, preserving Mon–Fri
        if (normalizedStart.getTime() === startOfWeek(from, 1).getTime() && normalizedEnd.getTime() === endOfWorkWeek(from, 1).getTime()) {
            const anchor = addDays(from, direction * 7);
            applyPeriod(startOfWeek(anchor, 1), endOfWorkWeek(anchor, 1));
            return;
        }

        const spanDays = Math.round((startOfDay(to).getTime() - startOfDay(from).getTime()) / 86_400_000) + 1;
        applyPeriod(addDays(from, direction * spanDays), addDays(to, direction * spanDays));
    };

    return {
        setPreset,
        jumpToDay,
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
    };
}
