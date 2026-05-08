import type { TimeEntryMutation } from "@/components/tracking/calendar/types";
import { ApiValidationException } from "@/util/ApiValidationError";
import { buildHandoffInteraction, useEventMutation } from "./useEventMutation";

export function useConflict() {
    const calendarStore = useCalendarStore();
    const { interaction } = storeToRefs(calendarStore);
    const mutation = useEventMutation();

    const finish = async (mutations: TimeEntryMutation[]) => {
        if (interaction.value.kind !== "conflict") return;

        const result = await mutation.executeAll(mutations);

        if (result.status === "success") {
            interaction.value = { kind: "idle" };
            return;
        }

        if (result.error instanceof ApiValidationException) {
            const handoff = buildHandoffInteraction(result.failedMutation, result.remaining, result.error.errors);
            if (handoff) {
                interaction.value = handoff;
                return;
            }
        }

        // Failure with no recoverable handoff (delete validation, network etc.):
        // roll back optimistic UI changes from mutations that never ran.
        mutation.cancelPending([result.failedMutation, ...result.remaining]);

        interaction.value = { kind: "idle" };
    };

    const cancel = async () => {
        if (interaction.value.kind !== "conflict") return;
        const { event, mutation: conflictMutation } = interaction.value;

        if (event.kind === "draft") {
            mutation.execute({ kind: "delete", event });
        }

        if ("originalPosition" in conflictMutation) {
            event.start = conflictMutation.originalPosition.start;
            event.end = conflictMutation.originalPosition.end;
        }

        interaction.value = { kind: "idle" };
    };

    return { finish, cancel };
}
