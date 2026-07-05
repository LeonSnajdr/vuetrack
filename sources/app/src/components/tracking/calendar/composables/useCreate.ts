import type { DraftTimeEntryEvent, SuggestionTimeEntryEvent } from "@/components/tracking/calendar/types";
import { ApiValidationException } from "@/util/ApiValidationError";
import { useCalendarHelper } from "./useCalendarHelper";
import { useEventMutation } from "./useEventMutation";

export function useCreate() {
    const calendarStore = useCalendarStore();
    const mutation = useEventMutation();
    const { buildCreateMutation } = useCalendarHelper();

    const { interaction } = storeToRefs(calendarStore);

    const start = (event: DraftTimeEntryEvent | SuggestionTimeEntryEvent) => {
        const createMutation = buildCreateMutation(event);
        interaction.value = { kind: "create", event, mutation: createMutation };
    };

    const finish = async () => {
        if (interaction.value.kind !== "create") return;

        const { event, mutation: createMutation, pendingMutations } = interaction.value;

        if (!pendingMutations && mutation.tryEnterConflict(event, createMutation)) return;

        const createResult = await mutation.execute(createMutation);

        if (createResult.status === "error") {
            if (createResult.error instanceof ApiValidationException) {
                interaction.value.errors = createResult.error.errors;
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
        if (interaction.value.kind !== "create") return;
        const { event, pendingMutations } = interaction.value;

        mutation.deleteIfDraft(event);
        mutation.cancelPending(pendingMutations);

        interaction.value = { kind: "idle" };
    };

    return { start, finish, cancel };
}
