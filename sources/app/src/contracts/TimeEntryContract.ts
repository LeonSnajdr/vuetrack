import type { Branded } from "typings/brand";
import type { ActivityContract, ActivityId } from "@/contracts/ActivityContract";
import type { ProjectContract, ProjectId } from "@/contracts/ProjectContract";

export type TimeEntryCreateContract = {
    taskId: string;
    startTime: Date;
    endTime: Date;
    projectId: ProjectId;
    activityId: ActivityId;
    comment: string | null;
};

export type TimeEntryId = Branded<number, "timeEntryId"> | null;

export type TimeEntryContract = {
    id: TimeEntryId;
    userId: number;
    taskId: string;
    project: ProjectContract;
    activity: ActivityContract;
    breakDetails: TimeEntryBreakContract | null;
    startTime: Date;
    endTime: Date;
    comment: string | null;
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
    comment: string | null;
};
