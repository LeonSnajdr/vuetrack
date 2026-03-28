import type { Branded } from "typings/brand";

export type ActivityId = Branded<number, "activityId">;

export type ActivityContract = {
    id: number;
    name: string;
};
