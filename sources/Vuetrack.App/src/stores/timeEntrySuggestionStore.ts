import type { TimeEntrySuggestionContract, TimeEntrySuggestionId, TimeEntrySuggestionUpdateContract } from "@/contracts/TimeEntrySuggestion";
import type { ActionResult } from "@/util/ActionResult";
import { omit } from "lodash";

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
    const { execute: executeGenerate, isLoading: isGenerating } = useAsyncTask(TimeEntrySuggestionService.generate, {
        cancelPolicy: "byKey",
        key: () => "generate"
    });
    const { execute: executeReload, isLoading: isReloading } = useAsyncTask(TimeEntrySuggestionService.reload);

    const executeLoadWithFilters = async () => {
        const currentFilter = filter.value;
        await executeLoad(currentFilter);
    };

    watch(filter, executeLoadWithFilters, { deep: true });

    const generate = async (): Promise<ActionResult> => {
        const currentFilter = filter.value;
        const generateResult = await executeGenerate(currentFilter);
        if (generateResult.status !== "success") return generateResult;

        await executeLoad(currentFilter);
        return success();
    };

    const reload = async (): Promise<ActionResult> => {
        const reloadResult = await executeReload(filter.value);
        if (reloadResult.status !== "success") return reloadResult;

        await executeLoadWithFilters();
        return success();
    };

    const update = async (id: TimeEntrySuggestionId, updateContract: TimeEntrySuggestionUpdateContract): Promise<ActionResult<TimeEntrySuggestionContract>> => {
        const updateResult = await executeUpdate(id, updateContract);

        if (updateResult.status === "success") {
            const existing = timeEntrySuggestions.value.find((x) => x.id === id);

            // Times come from the request: those are the ones the backend accepted,
            // so nothing depends on how the response spells its dates.
            const serverFields = omit(updateResult.data, "dateStarted", "dateEnded");

            if (existing) {
                Object.assign(existing, serverFields);
                existing.dateStarted = new Date(updateContract.dateStarted);
                existing.dateEnded = new Date(updateContract.dateEnded);
            }
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
        generate,
        isGenerating,
        reload,
        isReloading,
        cancelPendingUpdate
    };
});
