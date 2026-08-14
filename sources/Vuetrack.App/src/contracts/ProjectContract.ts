import type { Branded } from "typings/brand";

export type ProjectId = Branded<string, "projectId">;

export type ProjectContract = {
    id: ProjectId;
    name: string;
};

export type ProjectLookupContract = {
    projectId: ProjectId | null;
};
