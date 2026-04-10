import type { Interaction, TimeEntryEvent } from "@/components/tracking/calendar/types";
import type { TimeEntryContract, TimeEntryCreateContract, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";
import type { TimeEntrySuggestionContract, TimeEntrySuggestionUpdateContract } from "@/contracts/TimeEntrySuggestion";
import type { Nullable } from "@/util/Nullable";
import type { CalendarInterval } from "./useCalendarInterval";

type RoundTimeOptions = {
    down?: boolean;
    snapPoints?: number[];
};

export const useCalendarHelper = () => {
    const timeEntryStore = useTimeEntryStore();
    const suggestionStore = useTimeEntrySuggestionStore();
    const calendarStore = useCalendarStore();
    const { intervalMinutes } = storeToRefs(calendarStore);

    const roundTime = (timeMs: number, options: RoundTimeOptions = {}): number => {
        const { down = true, snapPoints = [] } = options;

        const getStepSizeMs = (): number => {
            const stepSizeMap: Record<CalendarInterval, number> = {
                60: 15,
                30: 10,
                10: 5,
                15: 5,
                5: 5
            };

            return stepSizeMap[intervalMinutes.value] * 60 * 1000;
        };

        const getRoundedTime = (stepSizeMs: number): number => {
            const offset = timeMs % stepSizeMs;
            if (offset === 0) return timeMs;
            return down ? timeMs - offset : timeMs + (stepSizeMs - offset);
        };

        const getSnappedTime = (stepSizeMs: number, roundedTime: number): number | undefined => {
            const roundedDistance = Math.abs(roundedTime - timeMs);
            let closestSnapPoint: number | undefined;

            for (const snapPoint of snapPoints) {
                if (down ? snapPoint > timeMs : snapPoint < timeMs) continue;

                const snapDistance = Math.abs(snapPoint - timeMs);
                if (snapDistance > stepSizeMs || snapDistance >= roundedDistance) continue;

                if (closestSnapPoint === undefined || snapDistance < Math.abs(closestSnapPoint - timeMs)) {
                    closestSnapPoint = snapPoint;
                }
            }

            return closestSnapPoint;
        };

        const stepSizeMs = getStepSizeMs();
        const roundedTime = getRoundedTime(stepSizeMs);
        const snappedTime = getSnappedTime(stepSizeMs, roundedTime);

        return snappedTime ?? roundedTime;
    };

    const getEventBoundaries = (subject: TimeEntryEvent, candidates: TimeEntryEvent[]): number[] => {
        return candidates.filter((other) => other.uiId !== subject.uiId).flatMap((other) => [other.start, other.end]);
    };

    const getOverlappingEvents = (subject: TimeEntryEvent, candidates: TimeEntryEvent[]): TimeEntryEvent[] => {
        return candidates.filter((other) => {
            if (other.uiId === subject.uiId) return false;
            return subject.start < other.end && subject.end > other.start;
        });
    };

    const getOriginalPositon = (event: TimeEntryEvent, cur: Interaction) => {
        return cur.kind === "conflict" && cur.event.uiId === event.uiId && "originalPosition" in cur.mutation
            ? cur.mutation.originalPosition
            : { start: event.start, end: event.end };
    };

    const cancelPendingUpdateForEvent = (event: TimeEntryEvent): void => {
        if (event.kind === "existing") {
            timeEntryStore.cancelPendingUpdate(event.timeEntry.id);
        } else if (event.kind === "suggestion") {
            suggestionStore.cancelPendingUpdate(event.timeEntry.id);
        }
    };

    const buildTimeEntryCreate = (source: Nullable<TimeEntryCreateContract>): Nullable<TimeEntryCreateContract> => {
        return withProxy({
            taskId: source.taskId,
            projectId: source.projectId,
            activityId: source.activityId,
            comment: source.comment
        })
            .from(source, "startTime", "endTime")
            .build();
    };

    const buildTimeEntryUpdate = (source: TimeEntryContract): TimeEntryUpdateContract => {
        return withProxy({
            taskId: source.taskId,
            projectId: source.project.id,
            activityId: source.activity.id,
            comment: source.comment
        })
            .from(source, "startTime", "endTime")
            .build();
    };

    const buildTimeEntrySuggestionUpdate = (source: TimeEntrySuggestionContract): TimeEntrySuggestionUpdateContract => {
        return withProxy({
            taskId: source.taskId,
            projectId: source.project.id,
            activityId: source.activity.id,
            comment: source.comment
        })
            .from(source, "startTime", "endTime")
            .build();
    };

    return {
        roundTime,
        getEventBoundaries,
        getOverlappingEvents,
        getOriginalPositon,
        cancelPendingUpdateForEvent,
        buildTimeEntryCreate,
        buildTimeEntryUpdate,
        buildTimeEntrySuggestionUpdate
    };
};
