import type { ExistingTimeEntryEvent, SuggestionTimeEntryEvent } from "@/components/tracking/calendar/types";
import { ApiValidationException } from "@/util/ApiValidationError";
import { useCalendarHelper } from "./useCalendarHelper";
import { useEventMutation } from "./useEventMutation";

export function useEdit() {
    const calendarStore = useCalendarStore();
    const mutation = useEventMutation();
    const { buildUpdateMutation, getOriginalPosition, restoreOriginalPosition } = useCalendarHelper();

    const { interaction } = storeToRefs(calendarStore);

    const start = (event: ExistingTimeEntryEvent | SuggestionTimeEntryEvent) => {
        const originalPosition = getOriginalPosition(event, interaction.value);
        const editMutation = buildUpdateMutation(event, originalPosition);
        interaction.value = { kind: "edit", event, mutation: editMutation };
    };

    const finish = async () => {
        if (interaction.value.kind !== "edit") return;

        const { event, mutation: editMutation, pendingMutations } = interaction.value;

        if (!pendingMutations && event.kind === "existing" && mutation.tryEnterConflict(event, editMutation)) return;

        const editResult = await mutation.execute(editMutation);

        if (editResult.status === "error") {
            if (editResult.error instanceof ApiValidationException) {
                interaction.value.errors = editResult.error.errors;
            }
            return;
        }

        if (pendingMutations?.length) {
            const shouldIdle = await mutation.drainPending(pendingMutations);
            if (!shouldIdle) return;
        }

        interaction.value = { kind: "idle" };
    };

    const cancel = () => {
        if (interaction.value.kind !== "edit") return;
        const { mutation: editMutation, pendingMutations } = interaction.value;

        restoreOriginalPosition(editMutation);
        mutation.cancelPending(pendingMutations);

        interaction.value = { kind: "idle" };
    };

    return { start, finish, cancel };
}
