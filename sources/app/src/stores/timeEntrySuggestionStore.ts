import type { TimeEntrySuggestionContract, TimeEntrySuggestionId, TimeEntrySuggestionUpdateContract } from "@/contracts/TimeEntrySuggestion";
import type { ActionResult } from "@/util/ActionResult";
import type { OriginalPosition } from "./timeEntryStore";

export const useTimeEntrySuggestionStore = defineStore("timeEntrySuggestion", () => {
    const { state: timeEntrySuggestions } = useAsyncState(TimeEntrySuggestionService.load, [], { immediate: true, shallow: false });
    const { execute: executeUpdate, isCancelledError, cancel: cancelPendingUpdate } = useCancellableUpdate<TimeEntrySuggestionId>();

    const update = async (
        id: TimeEntrySuggestionId,
        updateContract: TimeEntrySuggestionUpdateContract,
        originalPosition?: OriginalPosition
    ): Promise<ActionResult<TimeEntrySuggestionContract>> => {
        try {
            const updated = await executeUpdate(id, (signal) => TimeEntrySuggestionService.update(id, updateContract, signal));
            const cur = timeEntrySuggestions.value.find((x) => x.id === id);
            Object.assign(cur!, updated);
            return success(updated);
        } catch (e) {
            if (isCancelledError(e)) return cancelled();
            console.error(e);
            if (originalPosition) {
                const cur = timeEntrySuggestions.value.find((x) => x.id === id);
                if (cur) {
                    cur.startTime = new Date(originalPosition.start);
                    cur.endTime = new Date(originalPosition.end);
                }
            }
            return error();
        }
    };

    const dismiss = async (id: TimeEntrySuggestionId): Promise<ActionResult> => {
        try {
            await TimeEntrySuggestionService.dismiss(id);
            timeEntrySuggestions.value = timeEntrySuggestions.value.filter((x) => x.id !== id);
            return success();
        } catch (e) {
            console.error(e);
            return error();
        }
    };

    return { timeEntrySuggestions, update, dismiss, cancelPendingUpdate };
});
