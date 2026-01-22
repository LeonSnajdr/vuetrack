import type {
    ExistingTimeEntryEvent,
    ExistingTimeEntryUpdateMutation,
    SuggestionTimeEntryEvent,
    SuggestionTimeEntryUpdateMutation
} from "@/components/tracking/calendar/types";
import { useEventMutation } from "./useEventMutation";
import { getOverlappingEvents } from "./shared";

export function useEdit() {
    const calendarStore = useCalendarStore();
    const mutation = useEventMutation();

    const { interaction, existingEvents, editLoading } = storeToRefs(calendarStore);

    const start = (event: ExistingTimeEntryEvent | SuggestionTimeEntryEvent) => {
        let editMutation: ExistingTimeEntryUpdateMutation | SuggestionTimeEntryUpdateMutation;
        if (event.kind === "existing") {
            editMutation = {
                kind: "update",
                event,
                update: withProxy({ taskId: event.timeEntry.taskId }).from(event.timeEntry, "startTime", "endTime").build(),
                originalPosition: { start: event.start, end: event.end }
            };
        } else {
            editMutation = {
                kind: "update",
                event,
                update: withProxy({ taskId: event.timeEntry.taskId }).from(event.timeEntry, "startTime", "endTime").build(),
                originalPosition: { start: event.start, end: event.end }
            };
        }

        interaction.value = { kind: "edit", event, mutation: editMutation };
    };

    const finish = async () => {
        if (interaction.value.kind !== "edit") return;

        const { event, mutation: editMutation } = interaction.value;

        if (event.kind === "existing") {
            const overlaps = getOverlappingEvents(event, existingEvents.value);

            if (overlaps.length > 0) {
                interaction.value = {
                    kind: "conflict",
                    event,
                    overlaps,
                    mutation: editMutation
                };
                return;
            }
        }

        editLoading.value = true;
        await mutation.execute(editMutation);
        editLoading.value = false;
        interaction.value = { kind: "idle" };
    };

    const cancel = () => {
        if (interaction.value.kind !== "edit") return;

        const { event, mutation: conflictMutation } = interaction.value;

        if ("originalPosition" in conflictMutation) {
            event.start = conflictMutation.originalPosition.start;
            event.end = conflictMutation.originalPosition.end;
        }

        interaction.value = { kind: "idle" };
    };

    return { start, finish, cancel };
}
