import type { TimeEntrySuggestionContract, TimeEntrySuggestionId, TimeEntrySuggestionUpdateContract } from "@/contracts/TimeEntrySuggestion";
import axios from "@/plugins/axios";

class TimeEntrySuggestionService {
    public async load(): Promise<TimeEntrySuggestionContract[]> {
        /*const result = await axios.api.get<TimeEntrySuggestionContract[]>(`timeEntrySuggestions`);
        return result.data;*/
        return [];
    }

    public async update(id: TimeEntrySuggestionId, updateContract: TimeEntrySuggestionUpdateContract, signal?: AbortSignal): Promise<void> {
        await axios.api.put<TimeEntrySuggestionContract>(`timeEntrySuggestions/${id}`, updateContract, { signal });
    }

    public async dismiss(id: TimeEntrySuggestionId): Promise<void> {
        await axios.api.delete(`timeEntrySuggestions/${id}`);
    }
}

export default new TimeEntrySuggestionService();
