import type { TimeEntrySuggestionId, TimeEntrySuggestionUpdateContract } from "@/contracts/TimeEntrySuggestion";
import type { ActionResult } from "@/util/ActionResult";

export const useTimeEntrySuggestionStore = defineStore("timeEntrySuggestion", () => {
    const {
        data: timeEntrySuggestions,
        execute: executeLoad,
        isLoading
    } = useAsyncState(TimeEntrySuggestionService.load, { initialValue: [], shallow: false });
    const {
        execute: executeUpdate,
        cancel: cancelPendingUpdate,
        isLoading: isUpdating
    } = useAsyncTask(TimeEntrySuggestionService.update, {
        cancelPolicy: "byKey",
        key: (x) => x.args[0]
    });
    const { execute: executeDismiss, isLoading: isDismissing } = useAsyncTask(TimeEntrySuggestionService.dismiss, {
        key: (x) => x.args[0]
    });

    const update = async (id: TimeEntrySuggestionId, updateContract: TimeEntrySuggestionUpdateContract): Promise<ActionResult> => {
        const updateResult = await executeUpdate(id, updateContract);

        if (updateResult.status === "success") {
            await executeLoad();
        }

        return updateResult;
    };

    const dismiss = async (id: TimeEntrySuggestionId): Promise<ActionResult> => {
        const dismissResult = await executeDismiss(id);

        if (dismissResult.status === "success") {
            await executeLoad();
        }

        return dismissResult;
    };

    return { timeEntrySuggestions, executeLoad, isLoading, update, isUpdating, dismiss, isDismissing, cancelPendingUpdate };
});
