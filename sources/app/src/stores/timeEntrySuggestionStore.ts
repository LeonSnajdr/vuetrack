import type { TimeEntrySuggestionId, TimeEntrySuggestionUpdateContract } from "@/contracts/TimeEntrySuggestion";

export const useTimeEntrySuggestionStore = defineStore("timeEntrySuggestion", () => {
    const { state: timeEntrySuggestions } = useAsyncState(TimeEntrySuggestionService.load, [], { immediate: true, shallow: false });

    const update = async (id: TimeEntrySuggestionId, updateContract: TimeEntrySuggestionUpdateContract): Promise<boolean> => {
        try {
            const updated = await TimeEntrySuggestionService.update(id, updateContract);
            const cur = timeEntrySuggestions.value.find((x) => x.id === id);
            Object.assign(cur!, updated);
            return true;
        } catch (e) {
            console.error(e);
            return false;
        }
    };

    const dismiss = async (id: TimeEntrySuggestionId): Promise<boolean> => {
        try {
            await TimeEntrySuggestionService.dismiss(id);
            timeEntrySuggestions.value = timeEntrySuggestions.value.filter((x) => x.id !== id);
            return true;
        } catch (e) {
            console.error(e);
            return false;
        }
    };

    return { timeEntrySuggestions, update, dismiss };
});
