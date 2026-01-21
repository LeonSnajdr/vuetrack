import type { TimeEntryEvent } from "@/components/tracking/calendar/types";
import { roundTime, getOverlappingEvents, cancelPendingUpdateForEvent } from "./shared";
import { useEventMutation } from "./useEventMutation";

export function useResize() {
    const calendarStore = useCalendarStore();
    const timeEntryStore = useTimeEntryStore();
    const suggestionStore = useTimeEntrySuggestionStore();
    const { interaction, existingEvents } = storeToRefs(calendarStore);
    const mutation = useEventMutation();

    const start = (event: TimeEntryEvent) => {
        if (event.kind !== "existing" && event.kind !== "suggestion") return;

        cancelPendingUpdateForEvent(event, timeEntryStore, suggestionStore);

        // Create Date objects that will be referenced by the mutation
        const startTimeRef = new Date(event.start);
        const endTimeRef = new Date(event.end);

        const resizeMutation = event.kind === "existing"
            ? {
                kind: "update" as const,
                event,
                update: {
                    startTime: startTimeRef,
                    endTime: endTimeRef,
                    taskId: event.timeEntry.taskId
                },
                originalPosition: { start: event.start, end: event.end }
              }
            : {
                kind: "update" as const,
                event,
                update: {
                    startTime: startTimeRef,
                    endTime: endTimeRef,
                    taskId: event.timeEntry.taskId
                },
                originalPosition: { start: event.start, end: event.end }
              };

        interaction.value = {
            kind: "resize",
            event,
            mutation: resizeMutation
        };
    };

    const update = (mouseMs: number) => {
        if (interaction.value.kind !== "resize") return;
        const { event, mutation: resizeMutation } = interaction.value;
        const mouseRounded = roundTime(mouseMs, false);
        event.end = Math.max(mouseRounded, event.start);

        // Update endTime Date reference only (startTime stays constant)
        resizeMutation.update.endTime.setTime(event.end);
    };

    const finish = async () => {
        if (interaction.value.kind !== "resize") return;
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

        interaction.value = { kind: "idle" };
        await mutation.execute(cur.mutation);
    };

    const cancel = () => {
        if (interaction.value.kind !== "resize") return;
        const cur = interaction.value;
        cur.event.start = cur.mutation.originalPosition.start;
        cur.event.end = cur.mutation.originalPosition.end;
        cur.mutation.update.startTime.setTime(cur.mutation.originalPosition.start);
        cur.mutation.update.endTime.setTime(cur.mutation.originalPosition.end);
        interaction.value = { kind: "idle" };
    };

    return { start, update, finish, cancel };
}
