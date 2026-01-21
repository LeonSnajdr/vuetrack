import type { TimeEntryMutation } from "@/components/tracking/calendar/types";

export function useEventMutation() {
    const calendarStore = useCalendarStore();
    const timeEntryStore = useTimeEntryStore();
    const suggestionStore = useTimeEntrySuggestionStore();
    const { draftEvents } = storeToRefs(calendarStore);

    const execute = async (mutation: TimeEntryMutation): Promise<ActionResult<void>> => {
        if (mutation.kind === "create") {
            const startTime = new Date(mutation.create.startTime);
            const endTime = new Date(mutation.create.endTime);

            if (mutation.event.kind === "draft") {
                if (!mutation.create.taskId) {
                    const idx = draftEvents.value.indexOf(mutation.event);
                    if (idx !== -1) draftEvents.value.splice(idx, 1);
                    return error();
                }

                const result = await timeEntryStore.create({
                    startTime,
                    endTime,
                    taskId: mutation.create.taskId
                });

                if (result.status === "success") {
                    const idx = draftEvents.value.indexOf(mutation.event);
                    if (idx !== -1) draftEvents.value.splice(idx, 1);
                }

                return result.status === "success" ? success(undefined) : error();
            } else if (mutation.event.kind === "suggestion") {
                const result = await timeEntryStore.create({
                    startTime,
                    endTime,
                    taskId: mutation.create.taskId
                });

                if (result.status === "success") {
                    await suggestionStore.dismiss(mutation.event.timeEntry.id);
                }

                return result.status === "success" ? success(undefined) : error();
            }

            return error();
        } else if (mutation.kind === "update") {
            const startTime = new Date(mutation.update.startTime);
            const endTime = new Date(mutation.update.endTime);

            let result: ActionResult<unknown>;
            if (mutation.event.kind === "existing") {
                result = await timeEntryStore.update(mutation.event.timeEntry.id, {
                    startTime,
                    endTime,
                    taskId: mutation.update.taskId
                });
            } else if (mutation.event.kind === "suggestion") {
                result = await suggestionStore.update(mutation.event.timeEntry.id, {
                    startTime,
                    endTime,
                    taskId: mutation.update.taskId
                });
            } else {
                return error();
            }

            if (result.status === "error") {
                mutation.event.start = mutation.originalPosition.start;
                mutation.event.end = mutation.originalPosition.end;
            }

            return result.status === "success" ? success(undefined) : error();
        } else if (mutation.kind === "delete") {
            if (mutation.event.kind === "draft") {
                const idx = draftEvents.value.indexOf(mutation.event);
                if (idx !== -1) draftEvents.value.splice(idx, 1);
                return success(undefined);
            } else if (mutation.event.kind === "existing") {
                const result = await timeEntryStore.remove(mutation.event.timeEntry.id);
                return result.status === "success" ? success(undefined) : error();
            } else if (mutation.event.kind === "suggestion") {
                const result = await suggestionStore.dismiss(mutation.event.timeEntry.id);
                return result.status === "success" ? success(undefined) : error();
            }
        }

        return error();
    };

    const executeAll = async (mutations: TimeEntryMutation[]): Promise<ActionResult<void>[]> => {
        return Promise.all(mutations.map((m) => execute(m)));
    };

    return { execute, executeAll };
}
