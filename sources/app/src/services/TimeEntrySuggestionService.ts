import type { ActivityId } from "@/contracts/ActivityContract";
import type { ProjectId } from "@/contracts/ProjectContract";
import type { TimeEntrySuggestionContract, TimeEntrySuggestionId, TimeEntrySuggestionUpdateContract } from "@/contracts/TimeEntrySuggestion";
import axios from "@/plugins/axios";

class TimeEntrySuggestionService {
    public async load(): Promise<TimeEntrySuggestionContract[]> {
        /*const result = await axios.api.get<TimeEntrySuggestionContract[]>(`timeEntrySuggestions`);
        return result.data;*/
        return [
            {
                taskId: "mySuggestion",
                activity: {
                    id: 4 as ActivityId,
                    name: "activity-3"
                },
                project: {
                    id: 5 as ProjectId,
                    name: "project-4"
                },
                id: 1 as TimeEntrySuggestionId,
                comment: "",
                endTime: new Date("2026-03-31T02:00:00"),
                startTime: new Date("2026-03-31T01:00:00")
            }
        ];
    }

    public async update(id: TimeEntrySuggestionId, updateContract: TimeEntrySuggestionUpdateContract, signal?: AbortSignal): Promise<TimeEntrySuggestionContract> {
        const result = await axios.api.put<TimeEntrySuggestionContract>(`timeEntrySuggestions/${id}`, updateContract, { signal });
        return result.data;
    }

    public async dismiss(id: TimeEntrySuggestionId): Promise<void> {
        await axios.api.delete(`timeEntrySuggestions/${id}`);
    }
}

export default new TimeEntrySuggestionService();
