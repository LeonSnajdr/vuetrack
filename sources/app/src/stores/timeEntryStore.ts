import type { TimeEntryContract, TimeEntryCreateContract, TimeEntryId, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";
import type { ActionResult } from "@/util/ActionResult";

export const useTimeEntryStore = defineStore("timeEntry", () => {
    const trackingStore = useTrackingStore();

    const { startTime, endTime } = storeToRefs(trackingStore);

    const { data: timeEntries, execute: executeLoad } = useAsyncState(TimeEntryService.load, { initialValue: [], shallow: false });
    const { execute: executeCreate } = useAsyncTask(TimeEntryService.create);
    const { execute: executeUpdate, cancel: cancelPendingUpdate } = useAsyncTask(TimeEntryService.update, {
        cancelPolicy: (x) => x.args[0]
    });
    const { execute: executeDelete } = useAsyncTask(TimeEntryService.delete);

    onMounted(() => {
        executeLoad({ startTime: startTime.value, endTime: endTime.value });
    });

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

    return { timeEntries, create, update, remove, cancelPendingUpdate };
});
