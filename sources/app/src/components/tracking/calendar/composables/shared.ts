import type { Interaction, TimeEntryEvent } from "@/components/tracking/calendar/types";
import type { TimeEntryContract, TimeEntryCreateContract, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";
import type { TimeEntrySuggestionContract, TimeEntrySuggestionUpdateContract } from "@/contracts/TimeEntrySuggestion";
import type { Nullable } from "@/util/Nullable";

type RoundTimeOptions = {
    down?: boolean;
    roundTo?: number;
    snapPoints?: number[];
};

export const roundTime = (time: number, options: RoundTimeOptions = {}): number => {
    const { down = true, roundTo = 15, snapPoints = [] } = options;
    const step = roundTo * 60 * 1000;
    const offset = time % step;
    const rounded = down ? time - offset : time + (step - offset);
    const roundedDistance = Math.abs(rounded - time);
    const snapped = snapPoints
        .filter((snapPoint) => (down ? snapPoint <= time : snapPoint >= time))
        .reduce<number | undefined>((closest, snapPoint) => {
            const snapDistance = Math.abs(snapPoint - time);
            if (snapDistance > step || snapDistance >= roundedDistance) return closest;
            if (closest === undefined) return snapPoint;

            return snapDistance < Math.abs(closest - time) ? snapPoint : closest;
        }, undefined);

    return snapped ?? rounded;
};

export const getEventBoundaries = (subject: TimeEntryEvent, candidates: TimeEntryEvent[]): number[] => {
    return candidates.filter((other) => other.uiId !== subject.uiId).flatMap((other) => [other.start, other.end]);
};

export const getOverlappingEvents = (subject: TimeEntryEvent, candidates: TimeEntryEvent[]): TimeEntryEvent[] => {
    return candidates.filter((other) => {
        if (other.uiId === subject.uiId) return false;
        return subject.start < other.end && subject.end > other.start;
    });
};

export const getOriginalPositon = (event: TimeEntryEvent, cur: Interaction) => {
    return cur.kind === "conflict" && cur.event.uiId === event.uiId && "originalPosition" in cur.mutation
        ? cur.mutation.originalPosition
        : { start: event.start, end: event.end };
};

export const cancelPendingUpdateForEvent = (
    event: TimeEntryEvent,
    timeEntryStore: ReturnType<typeof useTimeEntryStore>,
    suggestionStore: ReturnType<typeof useTimeEntrySuggestionStore>
): void => {
    if (event.kind === "existing") {
        timeEntryStore.cancelPendingUpdate(event.timeEntry.id);
    } else if (event.kind === "suggestion") {
        suggestionStore.cancelPendingUpdate(event.timeEntry.id);
    }
};

export const buildTimeEntryCreate = (source: Nullable<TimeEntryCreateContract>): Nullable<TimeEntryCreateContract> => {
    return withProxy({
        taskId: source.taskId,
        projectId: source.projectId,
        activityId: source.activityId,
        comment: source.comment
    })
        .from(source, "startTime", "endTime")
        .build();
};

export const buildTimeEntryUpdate = (source: TimeEntryContract): TimeEntryUpdateContract => {
    return withProxy({
        taskId: source.taskId,
        projectId: source.project.id,
        activityId: source.activity.id,
        comment: source.comment
    })
        .from(source, "startTime", "endTime")
        .build();
};

export const buildTimeEntrySuggestionUpdate = (source: TimeEntrySuggestionContract): TimeEntrySuggestionUpdateContract => {
    return withProxy({
        taskId: source.taskId,
        projectId: source.project.id,
        activityId: source.activity.id,
        comment: source.comment
    })
        .from(source, "startTime", "endTime")
        .build();
};
