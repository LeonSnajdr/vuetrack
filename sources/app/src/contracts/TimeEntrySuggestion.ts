import type { Branded } from "typings/brand";
import type { ActivityContract, ActivityId } from "@/contracts/ActivityContract";
import type { ProjectContract, ProjectId } from "@/contracts/ProjectContract";

export type TimeEntrySuggestionId = Branded<number, "timeEntrySuggestionId">;

export type TimeEntrySuggestionContract = {
    id: TimeEntrySuggestionId;
    taskId: string | null;
    dateStarted: Date;
    dateEnded: Date;
    project: ProjectContract | null;
    activity: ActivityContract | null;
    comment: string | null;
};

export type TimeEntrySuggestionUpdateContract = {
    taskId: string | null;
    dateStarted: Date;
    dateEnded: Date;
    projectId: ProjectId | null;
    activityId: ActivityId | null;
    comment: string | null;
};
