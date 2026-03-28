import type { TimeEntryContract, TimeEntryCreateContract, TimeEntryId, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";
import type { ActionResult } from "@/util/ActionResult";

export const useTimeEntryStore = defineStore("timeEntry", () => {
    const trackingStore = useTrackingStore();

    const { startTime, endTime } = storeToRefs(trackingStore);

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
        await executeLoad({ from: startTime.value, to: endTime.value });
    };

    watch([startTime, endTime], executeLoadWithFilters);

    const create = async (createContract: TimeEntryCreateContract): Promise<ActionResult<TimeEntryContract>> => {
        const createResult = await executeCreate(createContract);

        if (createResult.status === "success") {
            timeEntries.value.push(createResult.data);
        }

        return createResult;
    };

    const update = async (id: TimeEntryId, updateContract: TimeEntryUpdateContract): Promise<ActionResult<TimeEntryContract>> => {
        const updateResult = await executeUpdate(id, updateContract);

        if (updateResult.status === "success") {
            const cur = timeEntries.value.find((x) => x.id === id);
            Object.assign(cur!, updateResult.data);
        }

        return updateResult;
    };

    const remove = async (id: TimeEntryId): Promise<ActionResult> => {
        const deleteResult = await executeDelete(id);

        if (deleteResult.status === "success") {
            timeEntries.value = timeEntries.value.filter((x) => x.id !== id);
        }

        return deleteResult;
    };

    return { timeEntries, executeLoadWithFilters, isLoading, create, isCreating, update, isUpdating, remove, isDeleting, cancelPendingUpdate };
});
