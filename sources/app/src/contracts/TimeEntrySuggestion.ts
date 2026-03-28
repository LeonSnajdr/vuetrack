import type { Branded } from "typings/brand";
import type { ProjectId, ActivityId } from "@/contracts/TimeEntryContract";

export type TimeEntrySuggestionId = Branded<string, "timeEntrySuggestionId">;

export type TimeEntrySuggestionContract = {
    id: TimeEntrySuggestionId;
    taskId: string;
    startTime: Date;
    endTime: Date;
    projectId: ProjectId;
    activityId: ActivityId;
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
