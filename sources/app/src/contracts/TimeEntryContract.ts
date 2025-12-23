import type { Branded } from "typings/brand";

export type TimeEntryCreateContract = {
    taskId: string;
    startTime: Date;
    endTime: Date;
};

export type TimeEntryId = Branded<string, "timeEntryId">;

export type TimeEntryContract = {
    id: TimeEntryId;
    user: string;
    taskId: string;
    startTime: Date;
    endTime: Date;
};
