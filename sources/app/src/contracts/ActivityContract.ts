import type { Branded } from "typings/brand";

export type ActivityId = Branded<number, "activityId"> | null;

export type ActivityContract = {
    id: ActivityId;
    name: string;
};
