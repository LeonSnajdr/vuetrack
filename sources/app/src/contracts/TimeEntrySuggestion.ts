import type { Branded } from "typings/brand";

export type TimeEntrySuggestionId = Branded<string, "timeEntryId">;

export type TimeEntrySuggestionContract = {
    id: TimeEntrySuggestionId;
    taskId: string;
    startTime: Date;
    endTime: Date;
};
