import type { TimeEntryMutation } from "@/components/tracking/calendar/types";
import { ApiValidationException } from "@/util/ApiValidationError";
import { useEventMutation } from "./useEventMutation";

export function useConflict() {
    const calendarStore = useCalendarStore();
    const { interaction } = storeToRefs(calendarStore);
    const mutation = useEventMutation();

    const finish = async (mutations: TimeEntryMutation[]) => {
        if (interaction.value.kind !== "conflict") return;
        const { mutation: conflictMutation } = interaction.value;

        const result = await mutation.executeAll(mutations);

        if (result.status === "success") {
            interaction.value = { kind: "idle" };
            return;
        }

        if (result.status === "error" && result.error instanceof ApiValidationException) {
            if (conflictMutation.kind === "update") {
                interaction.value = {
                    kind: "edit",
                    event: conflictMutation.event,
                    mutation: conflictMutation,
                    errors: result.error.errors
                };
            } else {
                interaction.value = {
                    kind: "create",
                    event: conflictMutation.event,
                    mutation: conflictMutation,
                    errors: result.error.errors
                };
            }
        }
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
