import type { Branded } from "typings/brand";
import type { ActivityId } from "@/contracts/ActivityContract";
import type { ProjectId } from "@/contracts/ProjectContract";

export type TimeEntryFilter = {
    from: Date;
    to: Date;
};

export type TimeEntryCreateContract = {
    taskId: string;
    startTime: Date;
    endTime: Date;
    projectId: ProjectId;
    activityId: ActivityId;
    comment: string;
};

export type TimeEntryId = Branded<number, "timeEntryId"> | null;

export type TimeEntryContract = {
    id: TimeEntryId;
    userId: number;
    taskId: string;
    projectId: ProjectId;
    activityId: ActivityId;
    breakDetails: TimeEntryBreakContract | null;
    startTime: Date;
    endTime: Date;
    comment: string;
};

export type TimeEntryBreakContract = {
    durationMillis: number;
    valid: boolean;
};

export type TimeEntryUpdateContract = {
    taskId: string;
    projectId: ProjectId;
    activityId: ActivityId;
    startTime: Date;
    endTime: Date;
    comment: string;
};
