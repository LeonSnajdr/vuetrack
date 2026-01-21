import type { TimeEntryEvent } from "@/components/tracking/calendar/types";
import { roundTime, getOverlappingEvents, cancelPendingUpdateForEvent } from "./shared";

export function useResize() {
    const calendarStore = useCalendarStore();
    const timeEntryStore = useTimeEntryStore();
    const suggestionStore = useTimeEntrySuggestionStore();
    const { interaction, existingEvents } = storeToRefs(calendarStore);

    const start = (event: TimeEntryEvent) => {
        cancelPendingUpdateForEvent(event, timeEntryStore, suggestionStore);

        interaction.value = {
            kind: "resize",
            event,
            originalEndMs: event.end
        };
    };

    const update = (mouseMs: number) => {
        if (interaction.value.kind !== "resize") return;
        const { event } = interaction.value;
        const mouseRounded = roundTime(mouseMs, false);
        event.end = Math.max(mouseRounded, event.start);
    };

    const finish = async () => {
        if (interaction.value.kind !== "resize") return;
        const cur = interaction.value;

        if (cur.event.kind === "existing") {
            const overlaps = getOverlappingEvents(cur.event, existingEvents.value);

            if (overlaps.length > 0) {
                const origStartMs = cur.event.start;
                const origEndMs = cur.originalEndMs;

                interaction.value = {
                    kind: "conflict",
                    event: cur.event,
                    overlaps,
                    onResolved: async (position) => {
                        await updateEvent(cur.event, position, { start: origStartMs, end: origEndMs });
                        interaction.value = { kind: "idle" };
                    },
                    onCanceled: async () => {
                        cur.event.end = origEndMs;
                        interaction.value = { kind: "idle" };
                    }
                };
                return;
            }
        }

        const origStartMs = cur.event.start;
        const origEndMs = cur.originalEndMs;
        interaction.value = { kind: "idle" };
        await updateEvent(cur.event, undefined, { start: origStartMs, end: origEndMs });
    };

    const cancel = () => {
        if (interaction.value.kind !== "resize") return;
        const cur = interaction.value;
        cur.event.end = cur.originalEndMs;
        interaction.value = { kind: "idle" };
    };

    const updateEvent = async (event: TimeEntryEvent, position?: { start: number; end: number }, originalPositionMs?: { start: number; end: number }) => {
        const startTime = new Date(position?.start ?? event.start);
        const endTime = new Date(position?.end ?? event.end);

        let result: ActionResult<unknown> = error();
        if (event.kind === "existing") {
            result = await timeEntryStore.update(event.timeEntry.id, { startTime, endTime, taskId: event.timeEntry.taskId });
        } else if (event.kind === "suggestion") {
            result = await suggestionStore.update(event.timeEntry.id, { startTime, endTime, taskId: event.timeEntry.taskId });
        }

        if (result.status === "error" && originalPositionMs) {
            event.start = originalPositionMs.start;
            event.end = originalPositionMs.end;
        }
        return result;
    };

    return { start, update, finish, cancel };
}
