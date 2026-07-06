import type { TimeEntrySuggestionContract, TimeEntrySuggestionId, TimeEntrySuggestionUpdateContract } from "@/contracts/TimeEntrySuggestion";
import type { ActionResult } from "@/util/ActionResult";

export const useTimeEntrySuggestionStore = defineStore("timeEntrySuggestion", () => {
    const { filter } = useTrackingFilter();

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
    const { execute: executeAccept, isLoading: isAccepting } = useAsyncTask(TimeEntrySuggestionService.accept, {
        key: (x) => x.args[0]
    });

    const executeLoadWithFilters = async () => {
        await executeLoad(filter.value);
    };

    watch(filter, executeLoadWithFilters, { deep: true });

    const recommendAgain = async (): Promise<void> => {
        await TimeEntrySuggestionService.recommendAgain();
        await executeLoadWithFilters();
    };

    const update = async (id: TimeEntrySuggestionId, updateContract: TimeEntrySuggestionUpdateContract): Promise<ActionResult<TimeEntrySuggestionContract>> => {
        const updateResult = await executeUpdate(id, updateContract);

        if (updateResult.status === "success") {
            const existing = timeEntrySuggestions.value.find((x) => x.id === id);
            if (existing) Object.assign(existing, updateResult.data);
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

    const accept = async (id: TimeEntrySuggestionId): Promise<ActionResult> => {
        const acceptResult = await executeAccept(id);

        if (acceptResult.status === "success") {
            timeEntrySuggestions.value = timeEntrySuggestions.value.filter((x) => x.id !== id);
        }

        return acceptResult;
    };

    return {
        timeEntrySuggestions,
        executeLoad,
        executeLoadWithFilters,
        isLoading,
        update,
        isUpdating,
        dismiss,
        isDismissing,
        accept,
        isAccepting,
        recommendAgain,
        cancelPendingUpdate
    };
});
