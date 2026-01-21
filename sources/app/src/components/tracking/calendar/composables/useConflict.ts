import type { TimeEntryEvent } from "@/components/tracking/calendar/types";
import type { ConflictResolutionResult, EventMutation } from "@/components/tracking/calendar/features/ConflictDialog.vue";

export function useConflict() {
    const calendarStore = useCalendarStore();
    const timeEntryStore = useTimeEntryStore();
    const suggestionStore = useTimeEntrySuggestionStore();
    const { interaction, draftEvents, conflictLoadingId } = storeToRefs(calendarStore);

    const finish = async (result: ConflictResolutionResult) => {
        if (interaction.value.kind !== "conflict") return;
        await applyMutations(result.mutations);
        await interaction.value.onResolved(result.position);
        conflictLoadingId.value = null;
    };

    const cancel = async () => {
        if (interaction.value.kind !== "conflict") return;
        await interaction.value.onCanceled();
    };

    const applyMutations = async (mutations?: EventMutation[]) => {
        const promises = (mutations ?? []).map(async (m) => {
            if (m.action === "remove") {
                await removeEvent(m.event);
            } else if (m.action === "update") {
                await updateEvent(m.event, { start: m.start, end: m.end });
            }
        });
        await Promise.all(promises);
    };

    const removeEvent = async (event: TimeEntryEvent) => {
        if (event.kind === "draft") {
            const idx = draftEvents.value.indexOf(event);
            if (idx !== -1) draftEvents.value.splice(idx, 1);
        } else if (event.kind === "existing") {
            await timeEntryStore.remove(event.timeEntry.id);
        } else if (event.kind === "suggestion") {
            await suggestionStore.dismiss(event.timeEntry.id);
        }
    };

    const updateEvent = async (event: TimeEntryEvent, position: { start: number; end: number }) => {
        const startTime = new Date(position.start);
        const endTime = new Date(position.end);

        if (event.kind === "existing") {
            await timeEntryStore.update(event.timeEntry.id, { startTime, endTime, taskId: event.timeEntry.taskId });
        } else if (event.kind === "suggestion") {
            await suggestionStore.update(event.timeEntry.id, { startTime, endTime, taskId: event.timeEntry.taskId });
        }
    };

    return { finish, cancel };
}
