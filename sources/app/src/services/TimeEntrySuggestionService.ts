import type {
    GenerateSuggestionsResultContract,
    TimeEntrySuggestionContract,
    TimeEntrySuggestionId,
    TimeEntrySuggestionUpdateContract
} from "@/contracts/TimeEntrySuggestion";
import type { TrackingFilter } from "@/models/TrackingFilter";
import axios from "@/plugins/axios";
import { formatISO } from "date-fns";

class TimeEntrySuggestionService {
    public load = async (filter: TrackingFilter, signal?: AbortSignal): Promise<TimeEntrySuggestionContract[]> => {
        const result = await axios.api.get<TimeEntrySuggestionContract[]>("suggestions", {
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
        const result = await axios.api.patch<TimeEntrySuggestionContract>(`suggestions/${id}`, updateContract, { signal });
        return result.data;
    };

    public dismiss = async (id: TimeEntrySuggestionId): Promise<void> => {
        await axios.api.post(`suggestions/${id}/dismiss`);
    };

    public accept = async (id: TimeEntrySuggestionId): Promise<void> => {
        await axios.api.post(`suggestions/${id}/accept`);
    };

    public generate = async (filter: TrackingFilter, signal?: AbortSignal): Promise<GenerateSuggestionsResultContract> => {
        const result = await axios.api.post<GenerateSuggestionsResultContract>("suggestions/generate", this.buildRangeRequest(filter), { signal });
        return result.data;
    };

    public reload = async (filter: TrackingFilter): Promise<GenerateSuggestionsResultContract> => {
        const result = await axios.api.post<GenerateSuggestionsResultContract>("suggestions/reload", this.buildRangeRequest(filter));
        return result.data;
    };

    private buildRangeRequest(filter: TrackingFilter) {
        return {
            from: this.formatFilterDate(filter.from),
            to: this.formatFilterDate(filter.to)
        };
    }

    private formatFilterDate(value: Date): string {
        return formatISO(value, { representation: "date" });
    }
}

export default new TimeEntrySuggestionService();
