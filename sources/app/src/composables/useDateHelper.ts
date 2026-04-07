export function useDateHelper() {
    // 1 = Monday
    const startOfWeek = (date: Date, weekStartsOn: 0 | 1 = 1) => {
        const d = new Date(date);
        d.setHours(0, 0, 0, 0);

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

    return { startOfWeek, endOfWorkWeek };
}
