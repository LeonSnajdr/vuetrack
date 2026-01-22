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
    const notify = useNotify();
    const { t } = useI18n();

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
            } else if (result.status === "error") {
                notify.error(t("action.add.error"), { timeout: 5000 });
            }
        } else {
            const result = await timeEntryStore.create(mutation.create);

            if (result.status === "success") {
                await suggestionStore.dismiss(mutation.event.timeEntry.id);
            } else if (result.status === "error") {
                notify.error(t("action.add.error"), { timeout: 5000 });
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
            notify.error(t("action.save.error"), { timeout: 5000 });
        }
    };

    const executeDelete = async (
        mutation: DraftTimeEntryDeleteMutation | ExistingTimeEntryDeleteMutation | SuggestionTimeEntryDeleteMutation
    ): Promise<void> => {
        let result: ActionResult = success();

        if (mutation.event.kind === "draft") {
            const idx = draftEvents.value.indexOf(mutation.event);
            if (idx !== -1) draftEvents.value.splice(idx, 1);
        } else if (mutation.event.kind === "existing") {
            result = await timeEntryStore.remove(mutation.event.timeEntry.id);
        } else {
            result = await suggestionStore.dismiss(mutation.event.timeEntry.id);
        }

        if (result.status === "error") {
            notify.error(t("action.delete.error"), { timeout: 5000 });
        }
    };

    return { execute, executeAll };
}
