import type {
    ExistingTimeEntryEvent,
    ExistingTimeEntryUpdateMutation,
    SuggestionTimeEntryEvent,
    SuggestionTimeEntryUpdateMutation
} from "@/components/tracking/calendar/types";
import { ApiValidationException } from "@/util/ApiValidationError";
import { useEventMutation } from "./useEventMutation";
import { useCalendarHelper } from "./useCalendarHelper";

export function useEdit() {
    const calendarStore = useCalendarStore();
    const mutation = useEventMutation();
    const { buildTimeEntryUpdate, buildTimeEntrySuggestionUpdate, getOriginalPositon, getOverlappingEvents } = useCalendarHelper();

    const { interaction, existingEvents } = storeToRefs(calendarStore);

    const start = (event: ExistingTimeEntryEvent | SuggestionTimeEntryEvent) => {
        const originalPosition = getOriginalPositon(event, interaction.value);

        let editMutation: ExistingTimeEntryUpdateMutation | SuggestionTimeEntryUpdateMutation;
        if (event.kind === "existing") {
            editMutation = {
                kind: "update",
                event,
                update: buildTimeEntryUpdate(event.timeEntry),
                originalPosition
            };
        } else {
            editMutation = {
                kind: "update",
                event,
                update: buildTimeEntrySuggestionUpdate(event.timeEntry),
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

        const editResult = await mutation.execute(editMutation);

        if (editResult.status === "success") {
            interaction.value = { kind: "idle" };
            return;
        }

        if (editResult.status === "error" && editResult.error instanceof ApiValidationException) {
            interaction.value.errors = editResult.error.errors;
        }
    };

    const cancel = () => {
        if (interaction.value.kind !== "edit") return;

        const { event, mutation: editMutation } = interaction.value;

        if ("originalPosition" in editMutation) {
            event.start = editMutation.originalPosition.start;
            event.end = editMutation.originalPosition.end;
        }

        interaction.value = { kind: "idle" };
    };

    return { start, finish, cancel };
}
