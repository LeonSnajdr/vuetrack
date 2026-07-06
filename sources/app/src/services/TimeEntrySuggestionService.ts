import type { TimeEntrySuggestionContract, TimeEntrySuggestionId, TimeEntrySuggestionUpdateContract } from "@/contracts/TimeEntrySuggestion";
import type { TrackingFilter } from "@/models/TrackingFilter";
import axios from "@/plugins/axios";
import { formatISO } from "date-fns";

class TimeEntrySuggestionService {
    public load = async (filter: TrackingFilter, signal?: AbortSignal): Promise<TimeEntrySuggestionContract[]> => {
        const result = await axios.api.get<TimeEntrySuggestionContract[]>("timeEntrySuggestions", {
            signal,
            params: {
                from: this.formatFilterDate(filter.from),
                to: this.formatFilterDate(filter.to)
            }
        });

        return result.data;
    };

    public update = async (
        id: TimeEntrySuggestionId,
        updateContract: TimeEntrySuggestionUpdateContract,
        signal?: AbortSignal
    ): Promise<TimeEntrySuggestionContract> => {
        const result = await axios.api.put<TimeEntrySuggestionContract>(`timeEntrySuggestions/${id}`, updateContract, { signal });
        return result.data;
    };

    public dismiss = async (id: TimeEntrySuggestionId): Promise<void> => {
        await axios.api.delete(`timeEntrySuggestions/${id}`);
    };

    public accept = async (id: TimeEntrySuggestionId): Promise<void> => {
        await axios.api.post(`timeEntrySuggestions/${id}/accept`);
    };

    public recommendAgain = async (): Promise<void> => {
        await axios.api.post("timeEntrySuggestions/recommendAgain");
    };

    private formatFilterDate(value: Date): string {
        return formatISO(value, { representation: "date" });
    }
}

export default new TimeEntrySuggestionService();
