export const useTimeEntryStore = defineStore("timeEntry", () => {
    const { state: timeEntries } = useAsyncState(TimeEntryService.load, [], { immediate: true });

    return { timeEntries };
});
