import type { Branded } from "typings/brand";

export type TimeEntrySuggestionId = Branded<string, "timeEntryId">;

export type TimeEntrySuggestionContract = {
    id: TimeEntrySuggestionId;
    taskId: string;
    start: number;
    end: number;
};
