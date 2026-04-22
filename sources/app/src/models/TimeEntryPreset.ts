import type { ActivityId } from "@/contracts/ActivityContract";
import type { ProjectId } from "@/contracts/ProjectContract";
import type { Branded } from "typings/brand";

export type TimeEntryPresetId = Branded<string, "timeEntryPresetId">;

export type TimeEntryPreset = {
    id: TimeEntryPresetId;
    name: string;
    taskId: string | null;
    durationMinutes: number | null;
    projectId: ProjectId | null;
    activityId: ActivityId | null;
};

export type TimeEntryPresetCreate = Omit<TimeEntryPreset, "id">;
