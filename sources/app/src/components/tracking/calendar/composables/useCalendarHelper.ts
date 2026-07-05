import type {
    DraftTimeEntryCreateMutation,
    DraftTimeEntryDeleteMutation,
    DraftTimeEntryEvent,
    EventEdge,
    EventPosition,
    ExistingTimeEntryDeleteMutation,
    ExistingTimeEntryEvent,
    ExistingTimeEntryUpdateMutation,
    Interaction,
    SuggestionTimeEntryCreateMutation,
    SuggestionTimeEntryDeleteMutation,
    SuggestionTimeEntryEvent,
    SuggestionTimeEntryUpdateMutation,
    TimeEntryEvent,
    TimeEntryMutation
} from "@/components/tracking/calendar/types";
import type { TimeEntryContract, TimeEntryCreateContract, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";
import type { TimeEntrySuggestionContract, TimeEntrySuggestionUpdateContract } from "@/contracts/TimeEntrySuggestion";
import type { Nullable } from "@/util/Nullable";
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
    const calendarStore = useCalendarStore();
    const { calendarSettings } = storeToRefs(settingsStore);
    const { interaction } = storeToRefs(calendarStore);

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

    const getOverlappingEvents = (subject: TimeEntryEvent, candidates: TimeEntryEvent[]): TimeEntryEvent[] => {
        return candidates.filter((other) => {
            if (other.uiId === subject.uiId) return false;
            return subject.start < other.end && subject.end > other.start;
        });
    };

    const getOriginalPosition = (event: TimeEntryEvent, cur: Interaction): EventPosition => {
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

    const buildTimeEntryCreateFromSuggestion = (source: TimeEntrySuggestionContract): Nullable<TimeEntryCreateContract> => {
        return withProxy({
            taskId: source.taskId,
            projectId: source.project.id,
            activityId: source.activity.id,
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

    const buildUpdateMutation = (
        event: ExistingTimeEntryEvent | SuggestionTimeEntryEvent,
        originalPosition: EventPosition
    ): ExistingTimeEntryUpdateMutation | SuggestionTimeEntryUpdateMutation => {
        if (event.kind === "existing") {
            return { kind: "update", event, update: buildTimeEntryUpdate(event.timeEntry), originalPosition };
        }
        return { kind: "update", event, update: buildTimeEntrySuggestionUpdate(event.timeEntry), originalPosition };
    };

    // Shared start-of-interaction prelude for move/resize/edit: filters out
    // unsupported event kinds, cancels any pending background update, snapshots
    // the original position, and returns a ready-to-use update mutation.
    const prepareUpdateMutation = (
        event: TimeEntryEvent
    ): ExistingTimeEntryUpdateMutation | SuggestionTimeEntryUpdateMutation | null => {
        if (event.kind !== "existing" && event.kind !== "suggestion") return null;
        cancelPendingUpdateForEvent(event);
        const originalPosition = getOriginalPosition(event, interaction.value);
        return buildUpdateMutation(event, originalPosition);
    };

    const buildCreateMutation = (
        event: DraftTimeEntryEvent | SuggestionTimeEntryEvent
    ): DraftTimeEntryCreateMutation | SuggestionTimeEntryCreateMutation => {
        if (event.kind === "draft") {
            return { kind: "create", event, create: buildTimeEntryCreate(event.createEntry) };
        }
        return { kind: "create", event, create: buildTimeEntryCreateFromSuggestion(event.timeEntry) };
    };

    const buildDeleteMutation = (
        event: TimeEntryEvent
    ): DraftTimeEntryDeleteMutation | ExistingTimeEntryDeleteMutation | SuggestionTimeEntryDeleteMutation => {
        if (event.kind === "draft") return { kind: "delete", event };
        if (event.kind === "existing") return { kind: "delete", event, id: event.timeEntry.id };
        return { kind: "delete", event, id: event.timeEntry.id };
    };

    const withMutationPosition = (mutation: TimeEntryMutation, start: number, end: number): TimeEntryMutation => {
        if (mutation.kind === "update") {
            return {
                ...mutation,
                update: { ...mutation.update, startTime: new Date(start), endTime: new Date(end) }
            };
        }
        if (mutation.kind === "create") {
            return {
                ...mutation,
                create: { ...mutation.create, startTime: new Date(start), endTime: new Date(end) }
            };
        }
        return mutation;
    };

    const applyEventPosition = (event: TimeEntryEvent, start: number, end: number): void => {
        event.start = start;
        event.end = end;
    };

    const restoreOriginalPosition = (mutation: TimeEntryMutation): void => {
        if ("originalPosition" in mutation) {
            applyEventPosition(mutation.event, mutation.originalPosition.start, mutation.originalPosition.end);
        }
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
        getOverlappingEvents,
        getOriginalPosition,
        cancelPendingUpdateForEvent,
        buildTimeEntryCreate,
        buildTimeEntryCreateFromSuggestion,
        buildTimeEntryUpdate,
        buildTimeEntrySuggestionUpdate,
        buildUpdateMutation,
        buildCreateMutation,
        buildDeleteMutation,
        prepareUpdateMutation,
        withMutationPosition,
        applyEventPosition,
        restoreOriginalPosition,
        minimumEventDurationMs,
        updateEventPosition
    };
};
