import type {
    CreatableEvent,
    EventEdge,
    EventPosition,
    PositionableEvent,
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

type UpdateEventPositionPatch = {
    start?: number;
    end?: number;
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

    const buildTimeEntryCreate = (source: TimeEntryCreatePayload): TimeEntryCreatePayload => {
        return withProxy({
            taskId: source.taskId,
            projectId: source.projectId,
            activityId: source.activityId,
            comment: source.comment
        })
            .from(source, "dateStarted", "dateEnded")
            .build();
    };

    const buildTimeEntryCreateFromSuggestion = (source: TimeEntrySuggestionContract): TimeEntryCreatePayload => {
        return withProxy({
            taskId: source.taskId,
            projectId: source.projectId,
            activityId: source.activityId,
            comment: source.comment
        })
            .from(source, "dateStarted", "dateEnded")
            .build();
    };

    const buildTimeEntryUpdate = (source: TimeEntryContract): TimeEntryUpdateContract => {
        return withProxy({
            taskId: source.taskId,
            projectId: source.project.id,
            activityId: source.activity.id,
            comment: source.comment
        })
            .from(source, "dateStarted", "dateEnded")
            .build();
    };

    const buildTimeEntrySuggestionUpdate = (source: TimeEntrySuggestionContract): TimeEntrySuggestionUpdateContract => {
        return withProxy({
            taskId: source.taskId,
            projectId: source.projectId,
            activityId: source.activityId,
            comment: source.comment
        })
            .from(source, "dateStarted", "dateEnded")
            .build();
    };

    // Builds the payload an update mutation sends. Date fields are live
    // accessors onto the backing contract, so the payload always carries the
    // event's current position.
    const buildUpdatePayload = (event: PositionableEvent): TimeEntryUpdatePayload => {
        if (event.kind === "existing") return buildTimeEntryUpdate(event.timeEntry);
        return buildTimeEntrySuggestionUpdate(event.timeEntry);
    };

    const buildCreatePayload = (event: CreatableEvent): TimeEntryCreatePayload => {
        if (event.kind === "draft") return buildTimeEntryCreate(event.createEntry);
        return buildTimeEntryCreateFromSuggestion(event.timeEntry);
    };

    const buildUpdateMutation = (event: PositionableEvent, payload?: TimeEntryUpdatePayload): TimeEntryUpdateMutation => {
        if (event.kind === "existing") {
            const update = (payload as TimeEntryUpdateContract | undefined) ?? buildTimeEntryUpdate(event.timeEntry);
            return { kind: "update", event, update };
        }
        const update = (payload as TimeEntrySuggestionUpdateContract | undefined) ?? buildTimeEntrySuggestionUpdate(event.timeEntry);
        return { kind: "update", event, update };
    };

    const buildCreateMutation = (event: CreatableEvent, payload?: TimeEntryCreatePayload): TimeEntryCreateMutation => {
        const create = payload ?? buildCreatePayload(event);
        if (event.kind === "draft") return { kind: "create", event, create };
        return { kind: "create", event, create };
    };

    const buildDeleteMutation = (event: TimeEntryEvent): TimeEntryDeleteMutation => {
        if (event.kind === "draft") return { kind: "delete", event };
        if (event.kind === "existing") return { kind: "delete", event, id: event.timeEntry.id };
        return { kind: "delete", event, id: event.timeEntry.id };
    };

    const applyEventPosition = (event: TimeEntryEvent, start: number, end: number): void => {
        event.start = start;
        event.end = end;
    };

    const minimumEventDurationMs = 60 * 1000;

    const updateEventPosition = (event: TimeEntryEvent, patch: UpdateEventPositionPatch, lock: EventEdge = "start"): void => {
        const nextStart = patch.start ?? event.start;
        const nextEnd = patch.end ?? event.end;

        let normalizedStart = nextStart;
        let normalizedEnd = nextEnd;

        if (normalizedEnd - normalizedStart < minimumEventDurationMs) {
            if (lock === "end") {
                normalizedStart = normalizedEnd - minimumEventDurationMs;
            } else {
                normalizedEnd = normalizedStart + minimumEventDurationMs;
            }
        }

        event.start = normalizedStart;
        event.end = normalizedEnd;
    };

    return {
        roundTime,
        getAllBoundaries,
        getEventBoundaries,
        isRangeOverlapping,
        isOverlapping,
        getOverlappingEvents,
        cancelPendingUpdateForEvent,
        buildTimeEntryCreate,
        buildTimeEntryCreateFromSuggestion,
        buildTimeEntryUpdate,
        buildTimeEntrySuggestionUpdate,
        buildUpdatePayload,
        buildCreatePayload,
        buildUpdateMutation,
        buildCreateMutation,
        buildDeleteMutation,
        applyEventPosition,
        minimumEventDurationMs,
        updateEventPosition
    };
};
