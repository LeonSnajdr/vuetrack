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
        const startTimeRef = new Date(event.start);
        const endTimeRef = new Date(event.end);

        let createMutation: DraftTimeEntryCreateMutation | SuggestionTimeEntryCreateMutation;

        if (event.kind === "draft") {
            createMutation = {
                kind: "create",
                event,
                create: {
                    taskId: event.createEntry.taskId,
                    startTime: startTimeRef,
                    endTime: endTimeRef
                },
                originalPosition: { start: event.start, end: event.end }
            };
        } else {
            createMutation = {
                kind: "create",
                event,
                create: {
                    taskId: event.timeEntry.taskId,
                    startTime: startTimeRef,
                    endTime: endTimeRef
                },
                originalPosition: { start: event.start, end: event.end }
            };
        }

        interaction.value = { kind: "create", event, mutation: createMutation };
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
