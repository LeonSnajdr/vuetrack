import type { TimeEntryCreateContract, TimeEntryId, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";
import type { ActionResult } from "@/util/ActionResult";

export const useTimeEntryStore = defineStore("timeEntry", () => {
    const trackingStore = useTrackingStore();

    const notify = useNotify();
    const { t } = useI18n();

    const { from, to } = storeToRefs(trackingStore);

    const { data: timeEntries, execute: executeLoad, isLoading } = useAsyncState(TimeEntryService.load, { initialValue: [], shallow: false });
    const { execute: executeCreate, isLoading: isCreating } = useAsyncTask(TimeEntryService.create);
    const {
        execute: executeUpdate,
        cancel: cancelPendingUpdate,
        isLoading: isUpdating
    } = useAsyncTask(TimeEntryService.update, {
        cancelPolicy: "byKey",
        key: (x) => x.args[0]
    });
    const { execute: executeDelete, isLoading: isDeleting } = useAsyncTask(TimeEntryService.delete, {
        key: (x) => x.args[0]
    });

    const executeLoadWithFilters = async () => {
        await executeLoad({ from: from.value, to: to.value });
    };

    watch([from, to], executeLoadWithFilters);

    const create = async (createContract: TimeEntryCreateContract): Promise<ActionResult> => {
        const createResult = await executeCreate(createContract);

        if (createResult.status === "error") {
            notify.error(t("action.create.error", { type: t("timeEntry.singular") }), { timeout: 5000 });
        }

        if (createResult.status === "success") {
            await executeLoadWithFilters();
        }

        return createResult;
    };

    const update = async (id: TimeEntryId, updateContract: TimeEntryUpdateContract): Promise<ActionResult> => {
        const updateResult = await executeUpdate(id, updateContract);

        if (updateResult.status === "error") {
            notify.error(t("action.save.error", { type: t("timeEntry.singular") }), { timeout: 5000 });
        }

        if (updateResult.status === "success") {
            await executeLoadWithFilters();
        }

        return updateResult;
    };

    const remove = async (id: TimeEntryId): Promise<ActionResult> => {
        const deleteResult = await executeDelete(id);

        if (deleteResult.status === "success") {
            await executeLoadWithFilters();
        }

        if (deleteResult.status === "error") {
            notify.error(t("action.delete.error", { type: t("timeEntry.singular") }), { timeout: 5000 });
        }

        return deleteResult;
    };

    return { timeEntries, executeLoadWithFilters, isLoading, create, isCreating, update, isUpdating, remove, isDeleting, cancelPendingUpdate };
});
