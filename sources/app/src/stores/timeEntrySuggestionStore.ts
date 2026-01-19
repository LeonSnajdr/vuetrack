import type { TimeEntrySuggestionContract, TimeEntrySuggestionId, TimeEntrySuggestionUpdateContract } from "@/contracts/TimeEntrySuggestion";
import type { ActionResult } from "@/util/ActionResult";

export const useTimeEntrySuggestionStore = defineStore("timeEntrySuggestion", () => {
    const { state: timeEntrySuggestions } = useAsyncState(TimeEntrySuggestionService.load, [], { immediate: true, shallow: false });
    const { execute: executeUpdate, cancel: cancelPendingUpdate } = useAsyncTask(TimeEntrySuggestionService.update, {
        cancelPolicy: (x) => x.args[0]
    });

    const update = async (id: TimeEntrySuggestionId, updateContract: TimeEntrySuggestionUpdateContract): Promise<ActionResult<TimeEntrySuggestionContract>> => {
        const updateResult = await executeUpdate(id, updateContract);

        if (updateResult.status === "success") {
            const cur = timeEntrySuggestions.value.find((x) => x.id === id);
            Object.assign(cur!, updateResult.data);
        }

        return updateResult;
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
