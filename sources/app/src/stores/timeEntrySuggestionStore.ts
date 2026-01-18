import type { TimeEntrySuggestionId, TimeEntrySuggestionUpdateContract } from "@/contracts/TimeEntrySuggestion";
import type { OriginalPosition } from "./timeEntryStore";

export const useTimeEntrySuggestionStore = defineStore("timeEntrySuggestion", () => {
    const { state: timeEntrySuggestions } = useAsyncState(TimeEntrySuggestionService.load, [], { immediate: true, shallow: false });
    const { execute: executeUpdate, isCancelledError } = useCancellableUpdate<TimeEntrySuggestionId>();

    const update = async (id: TimeEntrySuggestionId, updateContract: TimeEntrySuggestionUpdateContract, originalPosition?: OriginalPosition): Promise<boolean> => {
        try {
            const updated = await executeUpdate(id, (signal) => TimeEntrySuggestionService.update(id, updateContract, signal));
            const cur = timeEntrySuggestions.value.find((x) => x.id === id);
            Object.assign(cur!, updated);
            return true;
        } catch (e) {
            if (isCancelledError(e)) return false;
            console.error(e);
            if (originalPosition) {
                const cur = timeEntrySuggestions.value.find((x) => x.id === id);
                if (cur) {
                    cur.startTime = new Date(originalPosition.start);
                    cur.endTime = new Date(originalPosition.end);
                }
            }
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
