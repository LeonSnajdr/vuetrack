import type { DraftTimeEntryEvent, SuggestionTimeEntryEvent, TimeEntryEvent } from "@/components/tracking/calendar/types";
import { getOverlappingEvents } from "./shared";

export function useCreate() {
    const calendarStore = useCalendarStore();
    const timeEntryStore = useTimeEntryStore();
    const suggestionStore = useTimeEntrySuggestionStore();
    const { interaction, existingEvents, draftEvents, createLoading } = storeToRefs(calendarStore);

    const start = (event: DraftTimeEntryEvent | SuggestionTimeEntryEvent) => {
        interaction.value = { kind: "create", event };
    };

    const update = () => {
        // no-op for create dialog
    };

    const finish = async (event: TimeEntryEvent) => {
        if (interaction.value.kind !== "create") return;
        if (event.kind === "existing") return;

        const overlaps = getOverlappingEvents(event, existingEvents.value);

        if (overlaps.length > 0) {
            interaction.value = {
                kind: "conflict",
                event,
                overlaps,
                onResolved: async (position) => {
                    await createEvent(event, position);
                    interaction.value = { kind: "idle" };
                },
                onCanceled: async () => {
                    interaction.value = { kind: "idle" };
                }
            };
            return;
        }

        createLoading.value = true;
        await createEvent(event);
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

    const createEvent = async (event: TimeEntryEvent, position?: { start: number; end: number }) => {
        const startTime = new Date(position?.start ?? event.start);
        const endTime = new Date(position?.end ?? event.end);

        if (event.kind === "draft") {
            if (!event.createEntry.taskId) {
                removeEvent(event);
                return false;
            }

            const result = await timeEntryStore.create({
                startTime,
                endTime,
                taskId: event.createEntry.taskId
            });

            removeEvent(event);
            return result.status === "success";
        } else if (event.kind === "suggestion") {
            const result = await timeEntryStore.create({
                startTime,
                endTime,
                taskId: event.timeEntry.taskId
            });

            if (result.status === "success") {
                await suggestionStore.dismiss(event.timeEntry.id);
            }
            return result.status === "success";
        }
        return false;
    };

    const removeEvent = (event: TimeEntryEvent) => {
        if (event.kind === "draft") {
            const idx = draftEvents.value.indexOf(event);
            if (idx !== -1) draftEvents.value.splice(idx, 1);
        }
    };

    return { start, update, finish, cancel };
}
