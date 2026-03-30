import type { TimeEntryContract, TimeEntryCreateContract, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";

export const createDefaultTimeEntry = (): TimeEntryCreateContract => {
    return {
        taskId: "",
        startTime: new Date(),
        endTime: new Date(),
        activityId: null,
        projectId: null,
        comment: ""
    };
};

export const createEditableTimeEntry = (source: TimeEntryContract): TimeEntryUpdateContract => {
    return {
        taskId: source.taskId,
        startTime: source.startTime,
        endTime: source.endTime,
        projectId: source.project.id,
        activityId: source.activity.id,
        comment: source.comment
    };
};
