import type { ExistingTimeEntryEvent, SuggestionTimeEntryEvent, TimeEntryEvent } from "@/components/tracking/calendar/types";

export function useEdit() {
    const calendarStore = useCalendarStore();
    const timeEntryStore = useTimeEntryStore();
    const suggestionStore = useTimeEntrySuggestionStore();
    const { interaction, editLoading } = storeToRefs(calendarStore);

    const start = (event: ExistingTimeEntryEvent | SuggestionTimeEntryEvent) => {
        interaction.value = { kind: "edit", event };
    };

    const finish = async (event: TimeEntryEvent) => {
        if (event.kind === "draft") return;

        editLoading.value = true;
        await updateEvent(event);
        editLoading.value = false;
        interaction.value = { kind: "idle" };
    };

    const cancel = () => {
        interaction.value = { kind: "idle" };
    };

    const updateEvent = async (event: TimeEntryEvent) => {
        const startTime = new Date(event.start);
        const endTime = new Date(event.end);

        let result: ActionResult<unknown> = error();
        if (event.kind === "existing") {
            result = await timeEntryStore.update(event.timeEntry.id, { startTime, endTime, taskId: event.timeEntry.taskId });
        } else if (event.kind === "suggestion") {
            result = await suggestionStore.update(event.timeEntry.id, { startTime, endTime, taskId: event.timeEntry.taskId });
        }

        return result;
    };

    return { start, finish, cancel };
}
