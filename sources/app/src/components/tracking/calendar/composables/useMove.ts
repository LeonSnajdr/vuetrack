import type { ExistingTimeEntryUpdateMutation, SuggestionTimeEntryUpdateMutation, TimeEntryEvent } from "@/components/tracking/calendar/types";
import { ApiValidationException } from "@/util/ApiValidationError";
import { useCalendarHelper } from "./useCalendarHelper";
import { useEventMutation } from "./useEventMutation";

export function useMove() {
    const calendarStore = useCalendarStore();
    const { interaction, existingEvents } = storeToRefs(calendarStore);
    const mutation = useEventMutation();
    const {
        roundTime,
        getOverlappingEvents,
        cancelPendingUpdateForEvent,
        getOriginalPositon,
        getEventBoundaries,
        buildTimeEntryUpdate,
        buildTimeEntrySuggestionUpdate,
        minimumEventDurationMs,
        updateEventPosition
    } = useCalendarHelper();

    const start = (event: TimeEntryEvent) => {
        if (event.kind !== "existing" && event.kind !== "suggestion") return;

        cancelPendingUpdateForEvent(event);

        const originalPosition = getOriginalPositon(event, interaction.value);

        let moveMutation: ExistingTimeEntryUpdateMutation | SuggestionTimeEntryUpdateMutation;
        if (event.kind === "existing") {
            moveMutation = {
                kind: "update",
                event,
                update: buildTimeEntryUpdate(event.timeEntry),
                originalPosition
            };
        } else {
            moveMutation = {
                kind: "update",
                event,
                update: buildTimeEntrySuggestionUpdate(event.timeEntry),
                originalPosition
            };
        }

        interaction.value = {
            kind: "move",
            event,
            pointerOffsetMs: undefined,
            mutation: moveMutation
        };
    };

    const setPointerOffset = (mouseMs: number) => {
        if (interaction.value.kind !== "move") return;
        if (interaction.value.pointerOffsetMs !== undefined) return;
        interaction.value.pointerOffsetMs = mouseMs - interaction.value.event.start;
    };

    const update = (mouseMs: number) => {
        if (interaction.value.kind !== "move") return;
        const { event, pointerOffsetMs } = interaction.value;
        if (pointerOffsetMs === undefined) return;

        const duration = Math.max(event.end - event.start, minimumEventDurationMs);
        const snapPoints = getEventBoundaries(event, existingEvents.value).flatMap((boundary) => [boundary, boundary - duration]);
        const newStart = roundTime(mouseMs - pointerOffsetMs, { snapPoints });

        updateEventPosition(event, { start: newStart, end: newStart + duration }, "start");
    };

    const finish = async () => {
        if (interaction.value.kind !== "move") return;
        const cur = interaction.value;

        if (cur.event.kind === "existing") {
            const overlaps = getOverlappingEvents(cur.event, existingEvents.value);

            if (overlaps.length > 0) {
                interaction.value = {
                    kind: "conflict",
                    event: cur.event,
                    overlaps,
                    mutation: cur.mutation
                };
                return;
            }
        }

        const moveResult = await mutation.execute(cur.mutation);

        if (moveResult.status === "success") {
            interaction.value = { kind: "idle" };
            return;
        }

        if (moveResult.status === "error" && moveResult.error instanceof ApiValidationException) {
            interaction.value = {
                kind: "edit",
                event: cur.event,
                mutation: cur.mutation,
                errors: moveResult.error.errors
            };
            return;
        }

        cur.event.start = cur.mutation.originalPosition.start;
        cur.event.end = cur.mutation.originalPosition.end;
        interaction.value = { kind: "idle" };
    };

    const cancel = () => {
        if (interaction.value.kind !== "move") return;

        const cur = interaction.value;

        cur.event.start = cur.mutation.originalPosition.start;
        cur.event.end = cur.mutation.originalPosition.end;

        interaction.value = { kind: "idle" };
    };

    return { start, setPointerOffset, update, finish, cancel };
}
