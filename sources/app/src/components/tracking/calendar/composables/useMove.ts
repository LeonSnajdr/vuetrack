import type { TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useCalendarHelper } from "./useCalendarHelper";
import { useEventMutation } from "./useEventMutation";

export function useMove() {
    const calendarStore = useCalendarStore();
    const { interaction, existingEvents } = storeToRefs(calendarStore);
    const mutation = useEventMutation();
    const { roundTime, getEventBoundaries, prepareUpdateMutation, minimumEventDurationMs, updateEventPosition, restoreOriginalPosition } =
        useCalendarHelper();

    const start = (event: TimeEntryEvent) => {
        const moveMutation = prepareUpdateMutation(event);
        if (!moveMutation) return;

        interaction.value = {
            kind: "move",
            event: moveMutation.event,
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

        const shouldIdle = await mutation.commitUpdate(interaction.value);
        if (shouldIdle) {
            interaction.value = { kind: "idle" };
        }
    };

    const cancel = () => {
        if (interaction.value.kind !== "move") return;
        restoreOriginalPosition(interaction.value.mutation);
        interaction.value = { kind: "idle" };
    };

    return { start, setPointerOffset, update, finish, cancel };
}
