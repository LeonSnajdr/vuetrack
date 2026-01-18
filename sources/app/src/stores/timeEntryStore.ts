import type { TimeEntryCreateContract, TimeEntryId, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";

export interface OriginalPosition {
    start: number;
    end: number;
}

export const useTimeEntryStore = defineStore("timeEntry", () => {
    const { state: timeEntries } = useAsyncState(TimeEntryService.load, [], { immediate: true, shallow: false });
    const { execute: executeUpdate, isCancelledError, cancel: cancelPendingUpdate } = useCancellableUpdate<TimeEntryId>();

    const create = async (createContract: TimeEntryCreateContract): Promise<boolean> => {
        try {
            const created = await TimeEntryService.create(createContract);
            timeEntries.value.push(created);
            return true;
        } catch (e) {
            console.error(e);
            return false;
        }
    };

    const update = async (id: TimeEntryId, updateContract: TimeEntryUpdateContract, originalPosition?: OriginalPosition): Promise<boolean | null> => {
        try {
            const updated = await executeUpdate(id, (signal) => TimeEntryService.update(id, updateContract, signal));
            const cur = timeEntries.value.find((x) => x.id === id);

            Object.assign(cur!, updated);
            return true;
        } catch (e) {
            if (isCancelledError(e)) return null;
            console.error(e);
            if (originalPosition) {
                const cur = timeEntries.value.find((x) => x.id === id);
                if (cur) {
                    cur.startTime = new Date(originalPosition.start);
                    cur.endTime = new Date(originalPosition.end);
                }
            }
            return false;
        }
    };

    const remove = async (id: TimeEntryId): Promise<boolean> => {
        try {
            await TimeEntryService.delete(id);

            timeEntries.value = timeEntries.value.filter((x) => x.id !== id);
            return true;
        } catch (e) {
            console.error(e);
            return false;
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
