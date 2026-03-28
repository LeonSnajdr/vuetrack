function startOfWeek(date: Date, weekStartsOn: 0 | 1 = 1) {
    const d = new Date(date);
    d.setHours(0, 0, 0, 0);

    const day = d.getDay(); // 0=Sun..6=Sat
    const diff = (day - weekStartsOn + 7) % 7;

    d.setDate(d.getDate() - diff);
    return d;
}

function endOfWorkWeek(date: Date, weekStartsOn: 0 | 1 = 1) {
    const start = startOfWeek(date, weekStartsOn);
    const end = new Date(start);
    end.setDate(start.getDate() + 4);
    end.setHours(23, 59, 59, 999);
    return end;
}

export const useTrackingStore = defineStore("tracking", () => {
    const now = new Date();

    const startTime = ref<Date>(startOfWeek(now, 1)); // 1 = Monday
    const endTime = ref<Date>(endOfWorkWeek(now, 1));

    return { startTime, endTime };
});
