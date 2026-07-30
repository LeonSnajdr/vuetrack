import type {
    CreatableEvent,
    EventEdge,
    EventPosition,
    PositionableEvent,
    SuggestionTimeEntryEvent,
    TimeEntryCreateMutation,
    TimeEntryCreatePayload,
    TimeEntryDeleteMutation,
    TimeEntryEvent,
    TimeEntryUpdateMutation,
    TimeEntryUpdatePayload
} from "@/components/tracking/calendar/types";
import type { TimeEntryContract, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";
import type { TimeEntrySuggestionContract, TimeEntrySuggestionUpdateContract } from "@/contracts/TimeEntrySuggestion";
import type { CalendarInterval } from "./useCalendarInterval";

type RoundTimeOptions = {
    down?: boolean;
    snapPoints?: number[];
};

export const useCalendarHelper = () => {
    const timeEntryStore = useTimeEntryStore();
    const suggestionStore = useTimeEntrySuggestionStore();
    const settingsStore = useSettingsStore();
    const { calendarSettings } = storeToRefs(settingsStore);

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

            return stepSizeMap[calendarSettings.value.intervalMinutes] * 60 * 1000;
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

    const getAllBoundaries = (events: TimeEntryEvent[]): number[] => {
        return events.flatMap((e) => [e.start, e.end]);
    };

    const getEventBoundaries = (subject: TimeEntryEvent, candidates: TimeEntryEvent[]): number[] => {
        return getAllBoundaries(candidates.filter((other) => other.uiId !== subject.uiId));
    };

    const isRangeOverlapping = (subject: EventPosition, other: EventPosition): boolean => {
        return subject.start < other.end && subject.end > other.start;
    };

    const isOverlapping = (subject: TimeEntryEvent, other: TimeEntryEvent): boolean => {
        if (other.uiId === subject.uiId) return false;
        return isRangeOverlapping(subject, other);
    };

    const getOverlappingEvents = (subject: TimeEntryEvent, candidates: TimeEntryEvent[]): TimeEntryEvent[] => {
        return candidates.filter((other) => isOverlapping(subject, other));
    };

    const cancelPendingUpdateForEvent = (event: TimeEntryEvent): void => {
        if (event.kind === "existing") {
            timeEntryStore.cancelPendingUpdate(event.timeEntry.id);
        } else if (event.kind === "suggestion") {
            suggestionStore.cancelPendingUpdate(event.timeEntry.id);
        }
    };

    const buildTimeEntryCreateFromSuggestion = (source: TimeEntrySuggestionContract): TimeEntryCreatePayload => {
        return {
            taskId: source.taskId,
            projectId: source.projectId,
            activityId: source.activityId,
            comment: source.comment,
            dateStarted: new Date(source.dateStarted),
            dateEnded: new Date(source.dateEnded)
        };
    };

    const buildTimeEntryUpdate = (source: TimeEntryContract): TimeEntryUpdateContract => {
        return {
            taskId: source.taskId,
            projectId: source.project.id,
            activityId: source.activity.id,
            comment: source.comment,
            dateStarted: new Date(source.dateStarted),
            dateEnded: new Date(source.dateEnded)
        };
    };

    const buildTimeEntrySuggestionUpdate = (source: TimeEntrySuggestionContract): TimeEntrySuggestionUpdateContract => {
        return {
            taskId: source.taskId,
            projectId: source.projectId,
            activityId: source.activityId,
            comment: source.comment,
            dateStarted: new Date(source.dateStarted),
            dateEnded: new Date(source.dateEnded)
        };
    };

    // A snapshot of what is saved, for the change set to propose changes on.
    const buildUpdatePayload = (event: PositionableEvent): TimeEntryUpdatePayload => {
        if (event.kind === "existing") return buildTimeEntryUpdate(event.timeEntry);
        return buildTimeEntrySuggestionUpdate(event.timeEntry);
    };

    const buildCreatePayload = (event: SuggestionTimeEntryEvent): TimeEntryCreatePayload => {
        return buildTimeEntryCreateFromSuggestion(event.timeEntry);
    };

    // What the backend holds right now. A draft is not saved at all.
    const getPersistedPosition = (event: TimeEntryEvent): EventPosition | null => {
        if (event.kind === "draft") return null;

        return { start: event.timeEntry.dateStarted.getTime(), end: event.timeEntry.dateEnded.getTime() };
    };

    const buildUpdateMutation = (event: PositionableEvent, payload: TimeEntryUpdatePayload): TimeEntryUpdateMutation => {
        if (event.kind === "existing") {
            const update = payload as TimeEntryUpdateContract;
            return { kind: "update", event, update };
        }

        const update = payload as TimeEntrySuggestionUpdateContract;
        return { kind: "update", event, update };
    };

    const buildCreateMutation = (event: CreatableEvent, create: TimeEntryCreatePayload): TimeEntryCreateMutation => {
        return { kind: "create", event, create };
    };

    const buildDeleteMutation = (event: PositionableEvent): TimeEntryDeleteMutation => {
        if (event.kind === "existing") return { kind: "delete", event, id: event.timeEntry.id };
        return { kind: "delete", event, id: event.timeEntry.id };
    };

    const minimumEventDurationMs = 60 * 1000;

    // The locked edge stays put while the other one is pushed out to the minimum.
    const clampPosition = (position: EventPosition, lock: EventEdge = "start"): EventPosition => {
        if (position.end - position.start >= minimumEventDurationMs) return position;
        if (lock === "end") return { start: position.end - minimumEventDurationMs, end: position.end };

        return { start: position.start, end: position.start + minimumEventDurationMs };
    };

    return {
        roundTime,
        getAllBoundaries,
        getEventBoundaries,
        isRangeOverlapping,
        isOverlapping,
        getOverlappingEvents,
        cancelPendingUpdateForEvent,
        buildUpdatePayload,
        buildCreatePayload,
        buildUpdateMutation,
        buildCreateMutation,
        buildDeleteMutation,
        getPersistedPosition,
        minimumEventDurationMs,
        clampPosition
    };
};
