import type { TimeEntrySuggestionId, TimeEntrySuggestionUpdateContract } from "@/contracts/TimeEntrySuggestion";

export const useTimeEntrySuggestionStore = defineStore("timeEntrySuggestion", () => {
    const { state: timeEntrySuggestions } = useAsyncState(TimeEntrySuggestionService.load, [], { immediate: true, shallow: false });

    const update = async (id: TimeEntrySuggestionId, updateContract: TimeEntrySuggestionUpdateContract) => {
        try {
            const updated = await TimeEntrySuggestionService.update(id, updateContract);
            const cur = timeEntrySuggestions.value.find((x) => x.id === id);
            Object.assign(cur!, updated);
        } catch (e) {
            console.error(e);
        }
    };

    const dismiss = async (id: TimeEntrySuggestionId) => {
        try {
            await TimeEntrySuggestionService.dismiss(id);
            timeEntrySuggestions.value = timeEntrySuggestions.value.filter((x) => x.id !== id);
        } catch (e) {
            console.error(e);
        }
    };

    return { timeEntrySuggestions, update, dismiss };
});
