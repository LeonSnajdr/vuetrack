import type {
    ExistingTimeEntryEvent,
    ExistingTimeEntryUpdateMutation,
    SuggestionTimeEntryEvent,
    SuggestionTimeEntryUpdateMutation
} from "@/components/tracking/calendar/types";
import { useEventMutation } from "./useEventMutation";
import { createEditableTimeEntryUpdate, createEditableTimeEntrySuggestion, getOriginalPositon, getOverlappingEvents } from "./shared";

export function useEdit() {
    const calendarStore = useCalendarStore();
    const mutation = useEventMutation();

    const { interaction, existingEvents } = storeToRefs(calendarStore);

    const start = (event: ExistingTimeEntryEvent | SuggestionTimeEntryEvent) => {
        const originalPosition = getOriginalPositon(event, interaction.value);

        let editMutation: ExistingTimeEntryUpdateMutation | SuggestionTimeEntryUpdateMutation;
        if (event.kind === "existing") {
            editMutation = {
                kind: "update",
                event,
                update: createEditableTimeEntryUpdate(event.timeEntry),
                originalPosition
            };
        } else {
            editMutation = {
                kind: "update",
                event,
                update: createEditableTimeEntrySuggestion(event.timeEntry),
                originalPosition
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

        await mutation.execute(editMutation);

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
