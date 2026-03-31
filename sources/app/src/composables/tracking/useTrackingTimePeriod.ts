export type TrackingPeriodPreset = "custom" | "last7Days" | "lastMonth" | "thisMonth" | "today" | "workweek" | "yesterday";

export function useTrackingTimePeriod() {
    const trackingStore = useTrackingStore();
    const { from, to } = storeToRefs(trackingStore);

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

        from.value = startOfDay(nextStart);
        to.value = endOfDay(nextEnd);
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
        from,
        to,
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
