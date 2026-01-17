import type { TimeEntryContract, TimeEntryCreateContract, TimeEntryId, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";
import axios from "@/plugins/axios";

class TimeEntryService {
    public async load(): Promise<TimeEntryContract[]> {
        const result = await axios.api.get<TimeEntryContract[]>(`timeEntries`);
        return result.data;
    }

    public async create(createContract: TimeEntryCreateContract): Promise<TimeEntryContract> {
        const result = await axios.api.post<TimeEntryContract>(`timeEntries`, createContract);
        return result.data;
    }

    public async update(id: TimeEntryId, updateContract: TimeEntryUpdateContract): Promise<TimeEntryContract> {
        const result = await axios.api.put<TimeEntryContract>(`timeEntries/${id}`, updateContract);
        return result.data;
    }

    public async delete(id: TimeEntryId): Promise<void> {
        await axios.api.delete<TimeEntryContract>(`timeEntries/${id}`);
    }
}

export default new TimeEntryService();
