export const useTimeEntryStore = defineStore("timeEntry", () => {
    const { state: timeEntries } = useAsyncState(TimeEntryService.load, [], { immediate: true, shallow: false });

    const add = () => {
        timeEntries.value.push({
            id: "myId" as TimeEntryId,
            user: "leon",
            startTime: new Date(),
            endTime: new Date(Date.now() + 60 * 60 * 1000),
            taskId: "myTask"
        });
    };

    const removeLast = () => {
        timeEntries.value.pop();
    };

    const addOneHour = () => {
        const entry = timeEntries.value[0];
        if (!entry) return;

        entry.endTime = new Date(entry.endTime.getTime() + 60 * 60 * 1000);
    };

    return { timeEntries, add, removeLast, addOneHour };
});
