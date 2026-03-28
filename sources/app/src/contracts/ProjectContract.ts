import type { Branded } from "typings/brand";

export type ProjectId = Branded<number, "projectId">;

export type ProjectContract = {
    id: number;
    name: string;
};
