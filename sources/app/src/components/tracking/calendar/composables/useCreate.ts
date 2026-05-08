import type {
    DraftTimeEntryEvent,
    DraftTimeEntryCreateMutation,
    SuggestionTimeEntryEvent,
    SuggestionTimeEntryCreateMutation
} from "@/components/tracking/calendar/types";
import { ApiValidationException } from "@/util/ApiValidationError";
import { useCalendarHelper } from "./useCalendarHelper";
import { buildHandoffInteraction, useEventMutation } from "./useEventMutation";

export function useCreate() {
    const calendarStore = useCalendarStore();
    const mutation = useEventMutation();
    const { buildTimeEntryCreate, buildTimeEntrySuggestionUpdate, getOverlappingEvents } = useCalendarHelper();

    const { interaction, existingEvents } = storeToRefs(calendarStore);

    const start = (event: DraftTimeEntryEvent | SuggestionTimeEntryEvent) => {
        let createMutation: DraftTimeEntryCreateMutation | SuggestionTimeEntryCreateMutation;
        if (event.kind === "draft") {
            createMutation = {
                kind: "create",
                event,
                create: buildTimeEntryCreate(event.createEntry)
            };
        } else {
            createMutation = {
                kind: "create",
                event,
                create: buildTimeEntrySuggestionUpdate(event.timeEntry)
            };
        }

        interaction.value = { kind: "create", event, mutation: createMutation };
    };

    const finish = async () => {
        if (interaction.value.kind !== "create") return;

        const { event, mutation: createMutation, pendingMutations } = interaction.value;

        if (!pendingMutations) {
            const overlaps = getOverlappingEvents(event, existingEvents.value);

            if (overlaps.length > 0) {
                interaction.value = {
                    kind: "conflict",
                    event,
                    overlaps,
                    mutation: createMutation
                };
                return;
            }
        }

        const createResult = await mutation.execute(createMutation);

        if (createResult.status === "error" && createResult.error instanceof ApiValidationException) {
            interaction.value.errors = createResult.error.errors;
            return;
        }

        if (createResult.status !== "success") return;

        if (pendingMutations && pendingMutations.length > 0) {
            const drainResult = await mutation.executeAll(pendingMutations);

            if (drainResult.status === "error" && drainResult.error instanceof ApiValidationException) {
                const handoff = buildHandoffInteraction(drainResult.failedMutation, drainResult.remaining, drainResult.error.errors);
                if (handoff) {
                    interaction.value = handoff;
                    return;
                }
            }
        }

        interaction.value = { kind: "idle" };
    };

    const cancel = () => {
        if (interaction.value.kind !== "create") return;
        const { event: ev, pendingMutations } = interaction.value;

        if (ev.kind === "draft") {
            mutation.execute({ kind: "delete", event: ev });
        }

        mutation.cancelPending(pendingMutations);

        interaction.value = { kind: "idle" };
    };

    return { start, finish, cancel };
}
