export const useTimeEntrySuggestionStore = defineStore("timeEntrySuggestion", () => {
    const { state: timeEntrySuggestions } = useAsyncState(TimeEntrySuggestionService.load, [], { immediate: true });

    return { timeEntries: timeEntrySuggestions };
});
