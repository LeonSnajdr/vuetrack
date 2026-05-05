import type { TimeEntryCreateContract, TimeEntryId, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";
import { type ActionResult } from "@/util/ActionResult";
import { type Nullable } from "@/util/Nullable";

export const useTimeEntryStore = defineStore("timeEntry", () => {
    const { filter } = useTrackingFilter();

    const notify = useNotify();
    const { t } = useI18n();

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
        await executeLoad(filter.value);
    };

    watch(filter, executeLoadWithFilters, { deep: true });

    const create = async (createContract: Nullable<TimeEntryCreateContract>): Promise<ActionResult> => {
        if (!isNonNullable(createContract)) return error();

        const createResult = await executeCreate(createContract as TimeEntryCreateContract);

        if (createResult.status === "success") {
            await executeLoadWithFilters();
        }

        return createResult;
    };

    const update = async (id: TimeEntryId, updateContract: TimeEntryUpdateContract): Promise<ActionResult> => {
        const updateResult = await executeUpdate(id, updateContract);

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
