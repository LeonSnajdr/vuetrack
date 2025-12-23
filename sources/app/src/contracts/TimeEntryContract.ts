import type { Branded } from "typings/brand";

export type TimeEntryCreateContract = {
    taskId: string;
    start: number;
    end: number;
};

export type TimeEntryId = Branded<string, "timeEntryId">;

export type TimeEntryContract = {
    id: TimeEntryId;
    user: string;
    taskId: string;
    start: number;
    end: number;
};
