import type { TimeEntryContract, TimeEntryCreateContract, TimeEntryId, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";
import axios from "@/plugins/axios";
import type { TrackingFilter } from "@/models/TrackingFilter";
import { format, parseISO } from "date-fns";

// The backend returns the clean TimeEntryContract; dates arrive as naive local ISO strings
// (no timezone), so we revive them here rather than relying on the axios date transform.
type TimeEntryResponse = Omit<TimeEntryContract, "dateStarted" | "dateEnded"> & {
    dateStarted: string;
    dateEnded: string;
};

class TimeEntryService {
    public load = async (filter: TrackingFilter, signal?: AbortSignal): Promise<TimeEntryContract[]> => {
        const result = await axios.api.get<TimeEntryResponse[]>("timeEntry", {
            signal,
            params: {
                from: this.formatFilterDate(filter.from),
                to: this.formatFilterDate(filter.to)
            }
        });

        return result.data.map((dto) => this.mapResponse(dto));
    };

    public create = async (createContract: TimeEntryCreateContract): Promise<TimeEntryContract> => {
        const result = await axios.api.post<TimeEntryResponse>("timeEntry", this.toPayload(createContract));
        return this.mapResponse(result.data);
    };

    public update = async (id: TimeEntryId, updateContract: TimeEntryUpdateContract, signal?: AbortSignal): Promise<TimeEntryContract> => {
        const result = await axios.api.put<TimeEntryResponse>(`timeEntry/${id}`, this.toPayload(updateContract), { signal });
        return this.mapResponse(result.data);
    };

    public delete = async (id: TimeEntryId): Promise<void> => {
        await axios.api.delete(`timeEntry/${id}`);
    };

    private mapResponse(dto: TimeEntryResponse): TimeEntryContract {
        return {
            ...dto,
            dateStarted: parseISO(dto.dateStarted),
            dateEnded: parseISO(dto.dateEnded)
        };
    }

    private toPayload(contract: TimeEntryCreateContract | TimeEntryUpdateContract) {
        return {
            taskId: contract.taskId,
            projectId: contract.projectId,
            activityId: contract.activityId,
            dateStarted: this.formatDateTime(contract.dateStarted),
            dateEnded: this.formatDateTime(contract.dateEnded),
            comment: contract.comment
        };
    }

    // Naive local ISO (no timezone) so the wall-clock time reaches the backend unshifted.
    private formatDateTime(value: Date): string {
        return format(value, "yyyy-MM-dd'T'HH:mm:ss");
    }

    private formatFilterDate(value: Date): string {
        return format(value, "yyyy-MM-dd");
    }
}

export default new TimeEntryService();
