import type { Branded } from "typings/brand";

export type ActivityId = Branded<string, "activityId">;

export type ActivityContract = {
    id: ActivityId;
    name: string;
};
