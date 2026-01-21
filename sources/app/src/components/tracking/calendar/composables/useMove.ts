import type { TimeEntryEvent } from "@/components/tracking/calendar/types";
import { roundTime, getOverlappingEvents, cancelPendingUpdateForEvent } from "./shared";

export function useMove() {
    const calendarStore = useCalendarStore();
    const timeEntryStore = useTimeEntryStore();
    const suggestionStore = useTimeEntrySuggestionStore();
    const { interaction, existingEvents } = storeToRefs(calendarStore);

    const start = (event: TimeEntryEvent) => {
        cancelPendingUpdateForEvent(event, timeEntryStore, suggestionStore);

        interaction.value = {
            kind: "move",
            event,
            pointerOffsetMs: undefined,
            originalStartMs: event.start,
            originalEndMs: event.end
        };
    };

    const setPointerOffset = (mouseMs: number) => {
        if (interaction.value.kind !== "move") return;
        if (interaction.value.pointerOffsetMs !== undefined) return;
        interaction.value.pointerOffsetMs = mouseMs - interaction.value.event.start;
    };

    const update = (mouseMs: number) => {
        if (interaction.value.kind !== "move") return;
        const { event, pointerOffsetMs } = interaction.value;
        if (pointerOffsetMs === undefined) return;

        const duration = event.end - event.start;
        const newStart = roundTime(mouseMs - pointerOffsetMs);
        event.start = newStart;
        event.end = newStart + duration;
    };

    const finish = async () => {
        if (interaction.value.kind !== "move") return;
        const cur = interaction.value;

        if (cur.event.kind === "existing") {
            const overlaps = getOverlappingEvents(cur.event, existingEvents.value);

            if (overlaps.length > 0) {
                const origStartMs = cur.originalStartMs;
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
                        cur.event.start = origStartMs;
                        cur.event.end = origEndMs;
                        interaction.value = { kind: "idle" };
                    }
                };
                return;
            }
        }

        const origStartMs = cur.originalStartMs;
        const origEndMs = cur.originalEndMs;
        interaction.value = { kind: "idle" };
        await updateEvent(cur.event, undefined, { start: origStartMs, end: origEndMs });
    };

    const cancel = () => {
        if (interaction.value.kind !== "move") return;
        const cur = interaction.value;
        cur.event.start = cur.originalStartMs;
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

    return { start, setPointerOffset, update, finish, cancel };
}
