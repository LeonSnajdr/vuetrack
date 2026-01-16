import type { TimeEntryContract, TimeEntryCreateContract, TimeEntryId, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";

class TimeEntryService {
    public async load(): Promise<TimeEntryContract[]> {
        await new Promise<void>((resolve) => setTimeout(resolve, 3000));

        return [
            {
                id: "myId" as TimeEntryId,
                user: "leon",
                startTime: new Date(),
                endTime: new Date(Date.now() + 60 * 60 * 1000),
                taskId: "myTask"
            }
        ];
    }

    public async create(createContract: TimeEntryCreateContract): Promise<TimeEntryContract> {
        await new Promise<void>((resolve) => setTimeout(resolve, 3000));

        console.log("create", createContract);

        return {
            id: "newTimeEntry" as TimeEntryId,
            user: "myUser",
            ...createContract
        };
    }

    public async update(id: TimeEntryId, updateContract: TimeEntryUpdateContract): Promise<TimeEntryContract> {
        await new Promise<void>((resolve) => setTimeout(resolve, 3000));
        console.log("update", id, updateContract);

        return {
            id: id,
            user: "myUser",
            ...updateContract
        };
    }

    public async delete(id: TimeEntryId): Promise<void> {
        await new Promise<void>((resolve) => setTimeout(resolve, 3000));

        console.log("delete", id);
    }
}

export default new TimeEntryService();
