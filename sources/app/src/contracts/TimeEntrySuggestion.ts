import type { Branded } from "typings/brand";
import type { ActivityContract, ActivityId } from "@/contracts/ActivityContract";
import type { ProjectContract, ProjectId } from "@/contracts/ProjectContract";

export type TimeEntrySuggestionId = Branded<string, "timeEntrySuggestionId">;

export type TimeEntrySuggestionContract = {
    id: TimeEntrySuggestionId;
    taskId: string;
    startTime: Date;
    endTime: Date;
    project: ProjectContract;
    activity: ActivityContract;
    comment: string;
};

export type TimeEntrySuggestionUpdateContract = {
    taskId: string;
    startTime: Date;
    endTime: Date;
    projectId: ProjectId;
    activityId: ActivityId;
    comment: string;
};
