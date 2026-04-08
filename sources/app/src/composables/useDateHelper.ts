export function useDateHelper() {
    const durationBetween = (start: Date, end: Date) => end.getTime() - start.getTime();

    const toHoursAndMinutes = (durationMillis: number) => {
        const totalMinutes = Math.max(0, Math.floor(durationMillis / (1000 * 60)));
        return {
            hours: Math.floor(totalMinutes / 60),
            minutes: totalMinutes % 60
        };
    };

    const formatDurationMillis = (durationMillis: number) => {
        const { hours, minutes } = toHoursAndMinutes(durationMillis);
        return `${hours}h ${minutes}m`;
    };

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

    // 1 = Monday
    const startOfWeek = (date: Date, weekStartsOn: 0 | 1 = 1) => {
        const d = startOfDay(date);

        const day = d.getDay(); // 0=Sun..6=Sat
        const diff = (day - weekStartsOn + 7) % 7;

        d.setDate(d.getDate() - diff);
        return d;
    };

    const endOfWorkWeek = (date: Date, weekStartsOn: 0 | 1 = 1) => {
        const start = startOfWeek(date, weekStartsOn);
        const end = new Date(start);
        end.setDate(start.getDate() + 4);
        end.setHours(23, 59, 59, 999);
        return end;
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

    const normalizeRange = (start: Date, end: Date) => ({
        start: start <= end ? start : end,
        end: start <= end ? end : start
    });

    return {
        durationBetween,
        formatDurationMillis,
        startOfDay,
        endOfDay,
        startOfWeek,
        endOfWorkWeek,
        addDays,
        startOfMonth,
        endOfMonth,
        sameDay,
        normalizeRange
    };
}
