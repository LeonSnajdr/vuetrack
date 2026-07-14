import type { Branded } from "typings/brand";
import type { ActivityId } from "@/contracts/ActivityContract";
import type { ProjectId } from "@/contracts/ProjectContract";

export type TimeEntrySuggestionId = Branded<string, "timeEntrySuggestionId">;

export type TimeEntrySuggestionSourceContract = {
    connectorKey: string;
    externalId: string;
    link: string | null;
};

export type TimeEntrySuggestionContract = {
    id: TimeEntrySuggestionId;
    title: string;
    taskId: string | null;
    projectId: ProjectId | null;
    activityId: ActivityId | null;
    dateStarted: Date;
    dateEnded: Date;
    comment: string | null;
    status: "Pending" | "Edited" | "Dismissed" | "Confirmed";
    sources: TimeEntrySuggestionSourceContract[];
    confidence: number;
};

export type TimeEntrySuggestionUpdateContract = {
    title: string;
    taskId: string | null;
    projectId: ProjectId | null;
    activityId: ActivityId | null;
    dateStarted: Date;
    dateEnded: Date;
    comment: string | null;
};

export type GenerateSuggestionsResultContract = {
    generatedCount: number;
    connectorOutcomes: {
        connectorKey: string;
        status: string;
        signalCount: number;
        error: string | null;
    }[];
};
