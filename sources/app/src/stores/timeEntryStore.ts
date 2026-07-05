import type { TimeEntryContract, TimeEntryCreateContract, TimeEntryId, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";
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

    const create = async (createContract: Nullable<TimeEntryCreateContract>): Promise<ActionResult<TimeEntryContract>> => {
        if (!isNonNullable(createContract)) return error();

        const createResult = await executeCreate(createContract as TimeEntryCreateContract);

        if (createResult.status === "success") {
            timeEntries.value.push(createResult.data);
        }

        return createResult;
    };

    const update = async (id: TimeEntryId, updateContract: TimeEntryUpdateContract): Promise<ActionResult<TimeEntryContract>> => {
        const updateResult = await executeUpdate(id, updateContract);

        if (updateResult.status === "success") {
            const existing = timeEntries.value.find((x) => x.id === id);
            if (existing) Object.assign(existing, updateResult.data);
        }

        return updateResult;
    };

    const remove = async (id: TimeEntryId): Promise<ActionResult> => {
        const deleteResult = await executeDelete(id);

        if (deleteResult.status === "success") {
            timeEntries.value = timeEntries.value.filter((x) => x.id !== id);
        }

        if (deleteResult.status === "error") {
            notify.error(t("action.delete.error", { type: t("timeEntry.singular") }), { timeout: 5000 });
        }

        return deleteResult;
    };

    return { timeEntries, executeLoadWithFilters, isLoading, create, isCreating, update, isUpdating, remove, isDeleting, cancelPendingUpdate };
});
