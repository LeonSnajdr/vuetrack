import type { Branded } from "typings/brand";

export type TimeEntryFilter = {
    startTime: Date;
    endTime: Date;
};

export type ProjectId = Branded<number, "projectId">;
export type ActivityId = Branded<number, "activityId">;

export type TimeEntryCreateContract = {
    taskId: string;
    startTime: Date;
    endTime: Date;
    projectId: ProjectId;
    activityId: ActivityId;
    comment: string;
};

export type TimeEntryId = Branded<number, "timeEntryId">;

export type TimeEntryContract = {
    id: TimeEntryId;
    userId: number;
    taskId: string;
    projectId: ProjectId;
    activityId: ActivityId;
    startTime: Date;
    endTime: Date;
    comment: string;
};

export type TimeEntryUpdateContract = {
    taskId: string;
    projectId: ProjectId;
    activityId: ActivityId;
    startTime: Date;
    endTime: Date;
    comment: string;
};
