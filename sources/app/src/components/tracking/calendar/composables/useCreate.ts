import type {
    DraftTimeEntryEvent,
    DraftTimeEntryCreateMutation,
    SuggestionTimeEntryEvent,
    SuggestionTimeEntryCreateMutation,
    TimeEntryEvent
} from "@/components/tracking/calendar/types";
import { getOverlappingEvents } from "./shared";
import { useEventMutation } from "./useEventMutation";

export function useCreate() {
    const calendarStore = useCalendarStore();
    const mutation = useEventMutation();
    const { interaction, existingEvents, draftEvents, createLoading } = storeToRefs(calendarStore);

    const start = (event: DraftTimeEntryEvent | SuggestionTimeEntryEvent) => {
        let createMutation: DraftTimeEntryCreateMutation | SuggestionTimeEntryCreateMutation;

        if (event.kind === "draft") {
            createMutation = {
                kind: "create",
                event,
                create: {
                    get startTime() {
                        return event.createEntry.startTime;
                    },
                    get endTime() {
                        return event.createEntry.endTime;
                    },
                    taskId: event.createEntry.taskId
                }
            } as DraftTimeEntryCreateMutation;
        } else if (event.kind === "suggestion") {
            createMutation = {
                kind: "create",
                event,
                create: {
                    get startTime() {
                        return event.timeEntry.startTime;
                    },
                    get endTime() {
                        return event.timeEntry.endTime;
                    },
                    taskId: event.timeEntry.taskId
                }
            } as SuggestionTimeEntryCreateMutation;
        }

        interaction.value = { kind: "create", event, mutation: createMutation! };
    };

    const finish = async (event: TimeEntryEvent) => {
        if (interaction.value.kind !== "create") return;
        if (event.kind === "existing") return;

        const { mutation: createMutation } = interaction.value;

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

        createLoading.value = true;
        await mutation.execute(createMutation);
        createLoading.value = false;
        interaction.value = { kind: "idle" };
    };

    const cancel = () => {
        if (interaction.value.kind !== "create") return;
        const ev = interaction.value.event;

        if (ev.kind === "draft") {
            removeEvent(ev);
        }

        interaction.value = { kind: "idle" };
    };

    const removeEvent = (event: TimeEntryEvent) => {
        if (event.kind === "draft") {
            const idx = draftEvents.value.indexOf(event);
            if (idx !== -1) draftEvents.value.splice(idx, 1);
        }
    };

    return { start, finish, cancel };
}
