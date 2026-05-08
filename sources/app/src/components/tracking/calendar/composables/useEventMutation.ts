import { success } from "@/util/ActionResult";
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

    const execute = async (mutation: TimeEntryMutation) => {
        switch (mutation.kind) {
            case "create":
                return await executeCreate(mutation);
            case "update":
                return await executeUpdate(mutation);
            case "delete":
                return await executeDelete(mutation);
        }
    };

    const executeAll = async (mutations: TimeEntryMutation[]) => {
        for (const m of mutations) {
            const result = await execute(m);
            if (result.status !== "success") return result;
        }
        return success();
    };

    const executeCreate = async (mutation: DraftTimeEntryCreateMutation | SuggestionTimeEntryCreateMutation) => {
        if (mutation.event.kind === "draft") {
            const result = await timeEntryStore.create(mutation.create);

            if (result.status === "success") {
                const idx = draftEvents.value.indexOf(mutation.event);
                if (idx !== -1) draftEvents.value.splice(idx, 1);
            }

            return result;
        } else {
            const result = await timeEntryStore.create(mutation.create);

            if (result.status === "success") {
                await suggestionStore.dismiss(mutation.event.timeEntry.id);
            }

            return result;
        }
    };

    const executeUpdate = async (mutation: ExistingTimeEntryUpdateMutation | SuggestionTimeEntryUpdateMutation) => {
        if (mutation.event.kind === "existing") {
            return await timeEntryStore.update(mutation.event.timeEntry.id, mutation.update);
        } else {
            return await suggestionStore.update(mutation.event.timeEntry.id, mutation.update);
        }
    };

    const executeDelete = async (mutation: DraftTimeEntryDeleteMutation | ExistingTimeEntryDeleteMutation | SuggestionTimeEntryDeleteMutation) => {
        if (mutation.event.kind === "draft") {
            const idx = draftEvents.value.indexOf(mutation.event);
            if (idx !== -1) draftEvents.value.splice(idx, 1);
            return success();
        } else if (mutation.event.kind === "existing") {
            return await timeEntryStore.remove(mutation.event.timeEntry.id);
        } else {
            return await suggestionStore.dismiss(mutation.event.timeEntry.id);
        }
    };

    return { execute, executeAll };
}
