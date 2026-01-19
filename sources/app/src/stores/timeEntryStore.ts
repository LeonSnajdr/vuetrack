import type { TimeEntryContract, TimeEntryCreateContract, TimeEntryId, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";
import type { ActionResult } from "@/util/ActionResult";

export const useTimeEntryStore = defineStore("timeEntry", () => {
    const { state: timeEntries } = useAsyncState(TimeEntryService.load, [], { immediate: true, shallow: false });
    const { execute: executeUpdate, cancel: cancelPendingUpdate } = useAsyncTask(TimeEntryService.update, (x) => x.args[0]);

    const create = async (createContract: TimeEntryCreateContract): Promise<ActionResult<TimeEntryContract>> => {
        try {
            const created = await TimeEntryService.create(createContract);
            timeEntries.value.push(created);
            return success(created);
        } catch (e) {
            console.error(e);
            return error();
        }
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
        try {
            await TimeEntryService.delete(id);

            timeEntries.value = timeEntries.value.filter((x) => x.id !== id);
            return success();
        } catch (e) {
            console.error(e);
            return error();
        }
    };

    const add = () => {
        timeEntries.value.push({
            id: "myId" as TimeEntryId,
            user: "leon",
            startTime: new Date(),
            endTime: new Date(Date.now() + 60 * 60 * 1000),
            taskId: "myTask"
        });
    };

    const removeLast = () => {
        timeEntries.value.pop();
    };

    const addOneHour = () => {
        const entry = timeEntries.value[0];
        if (!entry) return;

        entry.endTime = new Date(entry.endTime.getTime() + 60 * 60 * 1000);
    };

    return { timeEntries, add, removeLast, addOneHour, create, update, remove, cancelPendingUpdate };
});
