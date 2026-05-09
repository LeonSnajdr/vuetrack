import type { TimeEntryEvent } from "@/components/tracking/calendar/types";
import { ApiValidationException } from "@/util/ApiValidationError";
import { useCalendarHelper } from "./useCalendarHelper";
import { useEventMutation } from "./useEventMutation";

export function useMove() {
    const calendarStore = useCalendarStore();
    const { interaction, existingEvents } = storeToRefs(calendarStore);
    const mutation = useEventMutation();
    const {
        roundTime,
        cancelPendingUpdateForEvent,
        getOriginalPosition,
        getEventBoundaries,
        buildUpdateMutation,
        minimumEventDurationMs,
        updateEventPosition,
        restoreOriginalPosition
    } = useCalendarHelper();

    const start = (event: TimeEntryEvent) => {
        if (event.kind !== "existing" && event.kind !== "suggestion") return;

        cancelPendingUpdateForEvent(event);

        const originalPosition = getOriginalPosition(event, interaction.value);
        const moveMutation = buildUpdateMutation(event, originalPosition);

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

        if (cur.event.kind === "existing" && mutation.tryEnterConflict(cur.event, cur.mutation)) return;

        const moveResult = await mutation.execute(cur.mutation);

        if (moveResult.status === "error" && moveResult.error instanceof ApiValidationException) {
            interaction.value = {
                kind: "edit",
                event: cur.event,
                mutation: cur.mutation,
                errors: moveResult.error.errors
            };
            return;
        }

        if (moveResult.status !== "success") {
            restoreOriginalPosition(cur.mutation);
        }

        interaction.value = { kind: "idle" };
    };

    const cancel = () => {
        if (interaction.value.kind !== "move") return;

        restoreOriginalPosition(interaction.value.mutation);

        interaction.value = { kind: "idle" };
    };

    return { start, setPointerOffset, update, finish, cancel };
}
