import type { TimeEntrySuggestionContract, TimeEntrySuggestionId, TimeEntrySuggestionUpdateContract } from "@/contracts/TimeEntrySuggestion";
import type { ActionResult } from "@/util/ActionResult";

export const useTimeEntrySuggestionStore = defineStore("timeEntrySuggestion", () => {
    const { data: timeEntrySuggestions, execute: executeLoad } = useAsyncState(TimeEntrySuggestionService.load, { initialValue: [], shallow: false });
    const { execute: executeUpdate, cancel: cancelPendingUpdate } = useAsyncTask(TimeEntrySuggestionService.update, {
        cancelPolicy: "byKey",
        key: (x) => x.args[0]
    });
    const { execute: executeDismiss } = useAsyncTask(TimeEntrySuggestionService.dismiss);

    onMounted(async () => {
        await executeLoad();
        console.log(timeEntrySuggestions.value);
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
        const dismissResult = await executeDismiss(id);

        if (dismissResult.status === "success") {
            timeEntrySuggestions.value = timeEntrySuggestions.value.filter((x) => x.id !== id);
        }

        return dismissResult;
    };

    return { timeEntrySuggestions, update, dismiss, cancelPendingUpdate };
});
