import type { Branded } from "typings/brand";
import type { ActivityContract, ActivityId } from "@/contracts/ActivityContract";
import type { ProjectContract, ProjectId } from "@/contracts/ProjectContract";

export type TimeEntryCreateContract = {
    taskId: string | null;
    dateStarted: Date;
    dateEnded: Date;
    projectId: ProjectId;
    activityId: ActivityId;
    comment: string | null;
};

export type TimeEntryId = Branded<number, "timeEntryId"> | null;

export type TimeEntryContract = {
    id: TimeEntryId;
    userId: number;
    taskId: string | null;
    project: ProjectContract;
    activity: ActivityContract;
    breakDetails: TimeEntryBreakContract | null;
    dateStarted: Date;
    dateEnded: Date;
    comment: string | null;
};

export type TimeEntryBreakContract = {
    durationMillis: number;
    valid: boolean;
};

export type TimeEntryUpdateContract = {
    taskId: string | null;
    projectId: ProjectId;
    activityId: ActivityId;
    dateStarted: Date;
    dateEnded: Date;
    comment: string | null;
};
