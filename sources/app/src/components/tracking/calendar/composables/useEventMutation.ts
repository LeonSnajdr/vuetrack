import type {
    DraftTimeEntryCreateMutation,
    DraftTimeEntryDeleteMutation,
    ExistingTimeEntryDeleteMutation,
    ExistingTimeEntryUpdateMutation,
    SuggestionTimeEntryCreateMutation,
    SuggestionTimeEntryDeleteMutation,
    SuggestionTimeEntryUpdateMutation,
    TimeEntryMutation
} from "@/components/tracking/calendar/types";

export function useEventMutation() {
    const calendarStore = useCalendarStore();
    const timeEntryStore = useTimeEntryStore();
    const suggestionStore = useTimeEntrySuggestionStore();

    const { draftEvents } = storeToRefs(calendarStore);

    const execute = async (mutation: TimeEntryMutation): Promise<void> => {
        switch (mutation.kind) {
            case "create":
                await executeCreate(mutation);
                break;
            case "update":
                await executeUpdate(mutation);
                break;
            case "delete":
                await executeDelete(mutation);
                break;
        }
    };

    const executeAll = async (mutations: TimeEntryMutation[]): Promise<void> => {
        await Promise.all(mutations.map((m) => execute(m)));
    };

    const executeCreate = async (mutation: DraftTimeEntryCreateMutation | SuggestionTimeEntryCreateMutation): Promise<void> => {
        if (mutation.event.kind === "draft") {
            if (!mutation.create.taskId) {
                const idx = draftEvents.value.indexOf(mutation.event);
                if (idx !== -1) draftEvents.value.splice(idx, 1);
                return;
            }

            const result = await timeEntryStore.create(mutation.create);

            if (result.status === "success") {
                const idx = draftEvents.value.indexOf(mutation.event);
                if (idx !== -1) draftEvents.value.splice(idx, 1);
            }
        } else {
            const result = await timeEntryStore.create(mutation.create);

            if (result.status === "success") {
                await suggestionStore.dismiss(mutation.event.timeEntry.id);
            }
        }
    };

    const executeUpdate = async (mutation: ExistingTimeEntryUpdateMutation | SuggestionTimeEntryUpdateMutation): Promise<void> => {
        let result: ActionResult<unknown>;
        if (mutation.event.kind === "existing") {
            result = await timeEntryStore.update(mutation.event.timeEntry.id, mutation.update);
        } else {
            result = await suggestionStore.update(mutation.event.timeEntry.id, mutation.update);
        }

        if (result.status === "error") {
            mutation.event.start = mutation.originalPosition.start;
            mutation.event.end = mutation.originalPosition.end;
        }
    };

    const executeDelete = async (
        mutation: DraftTimeEntryDeleteMutation | ExistingTimeEntryDeleteMutation | SuggestionTimeEntryDeleteMutation
    ): Promise<void> => {
        if (mutation.event.kind === "draft") {
            const idx = draftEvents.value.indexOf(mutation.event);
            if (idx !== -1) draftEvents.value.splice(idx, 1);
        } else if (mutation.event.kind === "existing") {
            await timeEntryStore.remove(mutation.event.timeEntry.id);
        } else {
            await suggestionStore.dismiss(mutation.event.timeEntry.id);
        }
    };

    return { execute, executeAll };
}
