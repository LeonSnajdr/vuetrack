import type { ExistingTimeEntryUpdateMutation, SuggestionTimeEntryUpdateMutation, TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useCalendarHelper } from "./useCalendarHelper";
import { useEventMutation } from "./useEventMutation";

export function useResize() {
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
        updateEventPosition
    } = useCalendarHelper();

    const start = (event: TimeEntryEvent) => {
        if (event.kind !== "existing" && event.kind !== "suggestion") return;

        cancelPendingUpdateForEvent(event);

        const originalPosition = getOriginalPositon(event, interaction.value);

        let resizeMutation: ExistingTimeEntryUpdateMutation | SuggestionTimeEntryUpdateMutation;
        if (event.kind === "existing") {
            resizeMutation = {
                kind: "update",
                event,
                update: buildTimeEntryUpdate(event.timeEntry),
                originalPosition
            };
        } else {
            resizeMutation = {
                kind: "update",
                event,
                update: buildTimeEntrySuggestionUpdate(event.timeEntry),
                originalPosition
            };
        }

        interaction.value = {
            kind: "resize",
            event,
            mutation: resizeMutation
        };
    };

    const update = (mouseMs: number) => {
        if (interaction.value.kind !== "resize") return;

        const { event } = interaction.value;
        const snapPoints = getEventBoundaries(event, existingEvents.value);
        const mouseRounded = roundTime(mouseMs, { down: false, snapPoints });

        updateEventPosition(event, { end: mouseRounded }, "start");
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

        interaction.value = { kind: "idle" };
    };

    return { start, update, finish, cancel };
}
