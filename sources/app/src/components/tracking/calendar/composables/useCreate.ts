import type {
    DraftTimeEntryEvent,
    DraftTimeEntryCreateMutation,
    SuggestionTimeEntryEvent,
    SuggestionTimeEntryCreateMutation
} from "@/components/tracking/calendar/types";
import { buildTimeEntryCreate, buildTimeEntrySuggestionUpdate, getOverlappingEvents } from "./shared";
import { useEventMutation } from "./useEventMutation";

export function useCreate() {
    const calendarStore = useCalendarStore();
    const mutation = useEventMutation();

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

        const { event, mutation: createMutation } = interaction.value;

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

        await mutation.execute(createMutation);

        interaction.value = { kind: "idle" };
    };

    const cancel = () => {
        if (interaction.value.kind !== "create") return;
        const ev = interaction.value.event;

        if (ev.kind === "draft") {
            mutation.execute({ kind: "delete", event: ev });
        }

        interaction.value = { kind: "idle" };
    };

    return { start, finish, cancel };
}
