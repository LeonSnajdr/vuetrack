import type { Branded } from "typings/brand";
import type { ActivityId } from "@/contracts/ActivityContract";
import type { ProjectId } from "@/contracts/ProjectContract";

export type TimeEntrySuggestionId = Branded<string, "timeEntrySuggestionId">;

export type TimeEntrySuggestionEvidenceContract = {
    connectorKey: string;
    externalId: string;
    kind: string;
    dateStarted: Date;
    dateEnded: Date;
    weight: number;
    metadata: Record<string, unknown>;
};

export type TimeEntrySuggestionContract = {
    id: TimeEntrySuggestionId;
    taskId: string | null;
    projectId: ProjectId | null;
    projectName: string | null;
    activityId: ActivityId | null;
    dateStarted: Date;
    dateEnded: Date;
    comment: string | null;
    status: "Pending" | "Edited" | "Dismissed" | "Confirmed";
    metadata: Record<string, unknown>;
    sources: TimeEntrySuggestionEvidenceContract[];
    confidence: number;
};

export type TimeEntrySuggestionUpdateContract = {
    taskId: string | null;
    projectId: ProjectId | null;
    activityId: ActivityId | null;
    dateStarted: Date;
    dateEnded: Date;
    comment: string | null;
};
