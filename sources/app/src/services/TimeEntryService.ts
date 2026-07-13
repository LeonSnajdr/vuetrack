import type { TimeEntryContract, TimeEntryCreateContract, TimeEntryId, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";
import type { ActivityId } from "@/contracts/ActivityContract";
import type { ProjectId } from "@/contracts/ProjectContract";
import axios from "@/plugins/axios";
import type { TrackingFilter } from "@/models/TrackingFilter";
import { ApiValidationException, type ApiValidationError, tryGetApiValidationError } from "@/util/ApiValidationError";

const validationFieldKeyMappings: ReadonlyArray<readonly [string, readonly string[]]> = [
    ["taskId", ["taskId"]],
    ["dateStarted", ["startDate", "startTime"]],
    ["dateEnded", ["endDate", "endTime"]],
    ["projectId", ["projectId", "project.id"]],
    ["activityId", ["activityId", "activity.id"]],
    ["comment", ["comment"]]
];

type ActivityDTO = {
    id: number;
    name: string;
};

type ProjectDTO = {
    id: number;
    name: string;
};

type BreakDTO = {
    durationMillis: number;
    valid: boolean;
};

type TimeEntryDTO = {
    timeEntryId: number | null;
    userId: number;
    createdByUserId: number | null;
    project: ProjectDTO;
    activity: ActivityDTO;
    breakDetails: BreakDTO | null;
    taskId: string;
    startDate: string;
    startTime: string;
    endDate: string;
    endTime: string;
    comment: string;
    approved: boolean;
};

class TimeEntryService {
    private readonly dtoById = new Map<TimeEntryId, TimeEntryDTO>();

    public load = async (filter: TrackingFilter, signal?: AbortSignal): Promise<TimeEntryContract[]> => {
        const result = await axios.api.get<TimeEntryDTO[]>("timeEntry", {
            signal,
            params: {
                from: this.formatFilterDate(filter.from),
                to: this.formatFilterDate(filter.to)
            }
        });

        const contracts = result.data.map((dto) => this.mapDtoToContract(dto));

        this.storeDtos(result.data);

        return contracts;
    };

    public create = async (createContract: TimeEntryCreateContract): Promise<TimeEntryContract> => {
        const knownIds = new Set(this.dtoById.keys());

        await this.invokeWithValidationMapping(() => axios.api.post<void>("timeEntry/upsert", this.mapContractToDto(createContract)));

        const contracts = await this.load({ from: createContract.dateStarted, to: createContract.dateEnded });
        const created = contracts.find((c) => !knownIds.has(c.id));
        if (!created) throw new Error("Created time entry not found after upsert");
        return created;
    };

    public update = async (id: TimeEntryId, updateContract: TimeEntryUpdateContract, signal?: AbortSignal): Promise<TimeEntryContract> => {
        await this.invokeWithValidationMapping(() => axios.api.post<void>("timeEntry/upsert", this.mapContractToDto(updateContract, id), { signal }));

        const contracts = await this.load({ from: updateContract.dateStarted, to: updateContract.dateEnded }, signal);
        const updated = contracts.find((c) => c.id === id);
        if (!updated) throw new Error(`Updated time entry ${id} not found after upsert`);
        return updated;
    };

    public delete = async (id: TimeEntryId): Promise<void> => {
        await axios.api.delete(`timeEntry`, {
            data: {
                idsToDelete: `${id}`
            }
        });
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

    private storeDtos(dtos: TimeEntryDTO[]): void {
        for (const dto of dtos) {
            this.dtoById.set(dto.timeEntryId as TimeEntryId, dto);
        }
    }

    private mapDtoToContract(dto: TimeEntryDTO): TimeEntryContract {
        return {
            id: dto.timeEntryId as TimeEntryId,
            userId: dto.userId,
            taskId: dto.taskId || null,
            project: {
                id: dto.project.id as ProjectId,
                name: dto.project.name
            },
            activity: {
                id: dto.activity.id as ActivityId,
                name: dto.activity.name
            },
            breakDetails: dto.breakDetails,
            dateStarted: this.combineDateAndTime(dto.startDate, dto.startTime),
            dateEnded: this.combineDateAndTime(dto.endDate, dto.endTime),
            comment: dto.comment
        };
    }

    private mapContractToDto(contract: TimeEntryCreateContract | TimeEntryUpdateContract, id?: TimeEntryId): TimeEntryDTO {
        const currentDto = id ? this.dtoById.get(id) : undefined;
        const startParts = this.splitDateAndTime(contract.dateStarted);
        const endParts = this.splitDateAndTime(contract.dateEnded);

        return {
            timeEntryId: id ? id : (currentDto?.timeEntryId ?? null),
            userId: currentDto?.userId ?? 3,
            createdByUserId: currentDto?.createdByUserId ?? null,
            project: { id: contract.projectId, name: "" },
            activity: { id: contract.activityId, name: "" },
            breakDetails: currentDto?.breakDetails ?? null,
            taskId: contract.taskId ?? "",
            startDate: startParts.date,
            startTime: startParts.time,
            endDate: endParts.date,
            endTime: endParts.time,
            comment: contract.comment ?? "",
            approved: currentDto?.approved ?? false
        };
    }

    private combineDateAndTime(date: string, time: string): Date {
        const [day, month, year] = date.split(".").map(Number);
        const [hour = 0, minute = 0, second = 0] = time.split(":").map(Number);

        return new Date(year, month - 1, day, hour, minute, second);
    }

    private splitDateAndTime(value: Date): { date: string; time: string } {
        const year = value.getFullYear();
        const month = (value.getMonth() + 1).toString().padStart(2, "0");
        const day = value.getDate().toString().padStart(2, "0");
        const hours = value.getHours().toString().padStart(2, "0");
        const minutes = value.getMinutes().toString().padStart(2, "0");

        return {
            date: `${day}.${month}.${year}`,
            time: `${hours}:${minutes}`
        };
    }

    private formatFilterDate(value: Date): string {
        const day = value.getDate().toString().padStart(2, "0");
        const month = (value.getMonth() + 1).toString().padStart(2, "0");
        const year = value.getFullYear();

        return `${day}.${month}.${year}`;
    }
}

export default new TimeEntryService();
