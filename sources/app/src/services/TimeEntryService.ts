import type { TimeEntryContract, TimeEntryCreateContract, TimeEntryFilter, TimeEntryId, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";
import type { ActivityId } from "@/contracts/ActivityContract";
import type { ProjectId } from "@/contracts/ProjectContract";
import axios from "@/plugins/axios";

type ActivityDTO = {
    id: number;
};

type ProjectDTO = {
    id: number;
};

type BreakDTO = {
    durationMillis: number;
    valid: boolean;
};

type TimeEntryDTO = {
    timeEntryId?: number;
    userId: number;
    createdByUserId?: number;
    project: ProjectDTO;
    activity: ActivityDTO;
    breakDetails?: BreakDTO;
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

    public load = async (filter: TimeEntryFilter): Promise<TimeEntryContract[]> => {
        const result = await axios.api.get<TimeEntryDTO[]>("timeEntry", {
            params: {
                from: this.formatFilterDate(filter.from),
                to: this.formatFilterDate(filter.to)
            }
        });

        const contracts = result.data.map((dto) => this.mapDtoToContract(dto));

        this.storeDtos(result.data);

        console.log(contracts);

        return contracts;
    };

    public create = async (createContract: TimeEntryCreateContract): Promise<TimeEntryContract> => {
        const result = await axios.api.post<TimeEntryDTO>("timeEntry/upsert", this.mapContractToDto(createContract));
        const contract = this.mapDtoToContract(result.data);

        this.storeDtos([result.data]);

        return contract;
    };

    public update = async (id: TimeEntryId, updateContract: TimeEntryUpdateContract, signal?: AbortSignal): Promise<TimeEntryContract> => {
        const result = await axios.api.post<TimeEntryDTO>("timeEntry/upsert", this.mapContractToDto(updateContract, id), { signal });
        const contract = this.mapDtoToContract(result.data);

        this.storeDtos([result.data]);

        return contract;
    };

    public delete = async (id: TimeEntryId): Promise<void> => {
        await axios.api.delete(`timeEntry`, {
            data: {
                idsToDelete: `${id}`
            }
        });
    };

    private storeDtos(dtos: TimeEntryDTO[]): void {
        for (const dto of dtos) {
            this.dtoById.set(dto.timeEntryId as TimeEntryId, dto);
        }
    }

    private mapDtoToContract(dto: TimeEntryDTO): TimeEntryContract {
        return {
            id: dto.timeEntryId as TimeEntryId,
            userId: dto.userId,
            taskId: dto.taskId,
            projectId: dto.project.id as ProjectId,
            activityId: dto.activity.id as ActivityId,
            startTime: this.combineDateAndTime(dto.startDate, dto.startTime),
            endTime: this.combineDateAndTime(dto.endDate, dto.endTime),
            comment: dto.comment
        };
    }

    private mapContractToDto(contract: TimeEntryCreateContract | TimeEntryUpdateContract, id?: TimeEntryId): TimeEntryDTO {
        const currentDto = id ? this.dtoById.get(id) : undefined;
        const startParts = this.splitDateAndTime(contract.startTime);
        const endParts = this.splitDateAndTime(contract.endTime);

        return {
            timeEntryId: id ? id : currentDto?.timeEntryId,
            userId: currentDto?.userId ?? 3,
            createdByUserId: currentDto?.createdByUserId,
            project: { id: contract.projectId! },
            activity: { id: contract.activityId! },
            breakDetails: currentDto?.breakDetails,
            taskId: contract.taskId,
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
