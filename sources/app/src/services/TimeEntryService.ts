import type { TimeEntryContract, TimeEntryCreateContract, TimeEntryId, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";
import axios from "@/plugins/axios";
import type { TrackingFilter } from "@/models/TrackingFilter";
import { ApiValidationException, type ApiValidationError, tryGetApiValidationError } from "@/util/ApiValidationError";
import { format, parseISO } from "date-fns";

const validationFieldKeyMappings: ReadonlyArray<readonly [string, readonly string[]]> = [
    ["taskId", ["taskId"]],
    ["dateStarted", ["dateStarted"]],
    ["dateEnded", ["dateEnded"]],
    ["projectId", ["projectId"]],
    ["activityId", ["activityId"]],
    ["comment", ["comment"]]
];

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
        const result = await this.invokeWithValidationMapping(() => axios.api.post<TimeEntryResponse>("timeEntry", this.toCreatePayload(createContract)));
        return this.mapResponse(result.data);
    };

    public update = async (id: TimeEntryId, updateContract: TimeEntryUpdateContract, signal?: AbortSignal): Promise<TimeEntryContract> => {
        const result = await this.invokeWithValidationMapping(() => axios.api.put<TimeEntryResponse>(`timeEntry/${id}`, this.toUpdatePayload(updateContract), { signal }));
        return this.mapResponse(result.data);
    };

    public delete = async (id: TimeEntryId): Promise<void> => {
        await axios.api.delete(`timeEntry/${id}`);
    };

    private async invokeWithValidationMapping<T>(fn: () => Promise<T>): Promise<T> {
        try {
            return await fn();
        } catch (error) {
            const validationError = this.tryMapValidationError(error);
            if (validationError) throw new ApiValidationException(validationError);
            throw error;
        }
    }

    private tryMapValidationError(error: unknown): ApiValidationError | null {
        const raw = tryGetApiValidationError(error);
        if (!raw) return null;

        const mapped: ApiValidationError = {};
        for (const [fieldKey, sourceKeys] of validationFieldKeyMappings) {
            const messages = [...new Set(sourceKeys.flatMap((sourceKey) => raw[sourceKey] ?? []))];
            if (messages.length > 0) mapped[fieldKey] = messages;
        }

        return Object.keys(mapped).length > 0 ? mapped : null;
    }

    private mapResponse(dto: TimeEntryResponse): TimeEntryContract {
        return {
            ...dto,
            dateStarted: parseISO(dto.dateStarted),
            dateEnded: parseISO(dto.dateEnded)
        };
    }

    private toCreatePayload(contract: TimeEntryCreateContract) {
        return {
            taskId: contract.taskId,
            projectId: contract.projectId,
            activityId: contract.activityId,
            dateStarted: this.formatDateTime(contract.dateStarted),
            dateEnded: this.formatDateTime(contract.dateEnded),
            comment: contract.comment
        };
    }

    private toUpdatePayload(contract: TimeEntryUpdateContract) {
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
