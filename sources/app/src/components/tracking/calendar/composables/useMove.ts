import type { TimeEntryEvent } from "@/components/tracking/calendar/types";
import { roundTime, getOverlappingEvents, cancelPendingUpdateForEvent } from "./shared";
import { useEventMutation } from "./useEventMutation";

export function useMove() {
    const calendarStore = useCalendarStore();
    const timeEntryStore = useTimeEntryStore();
    const suggestionStore = useTimeEntrySuggestionStore();
    const { interaction, existingEvents } = storeToRefs(calendarStore);
    const mutation = useEventMutation();

    const start = (event: TimeEntryEvent) => {
        if (event.kind !== "existing" && event.kind !== "suggestion") return;

        cancelPendingUpdateForEvent(event, timeEntryStore, suggestionStore);

        const moveMutation =
            event.kind === "existing"
                ? {
                      kind: "update" as const,
                      event,
                      update: {
                          get startTime() {
                              return event.timeEntry.startTime;
                          },
                          get endTime() {
                              return event.timeEntry.endTime;
                          },
                          taskId: event.timeEntry.taskId
                      },
                      originalPosition: { start: event.start, end: event.end }
                  }
                : {
                      kind: "update" as const,
                      event,
                      update: {
                          get startTime() {
                              return event.timeEntry.startTime;
                          },
                          get endTime() {
                              return event.timeEntry.endTime;
                          },
                          taskId: event.timeEntry.taskId
                      },
                      originalPosition: { start: event.start, end: event.end }
                  };

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

        const duration = event.end - event.start;
        const newStart = roundTime(mouseMs - pointerOffsetMs);

        event.start = newStart;
        event.end = newStart + duration;
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

        interaction.value = { kind: "idle" };
        await mutation.execute(cur.mutation);
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
