import { success } from "@/util/ActionResult";
import type { ValidationErrors } from "@/util/ValidationProblem";
import {
    isExistingUpdateMutation,
    type TimeEntryCreateMutation,
    type TimeEntryDeleteMutation,
    type TimeEntryMutation,
    type TimeEntryUpdateMutation
} from "@/components/tracking/calendar/types";

export type ExecuteAllResult =
    | { status: "success" }
    | { status: "cancelled" }
    | {
          status: "error";
          error?: unknown;
          validation?: ValidationErrors | null;
          failedMutation: TimeEntryMutation;
          remaining: TimeEntryMutation[];
      };

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

    // Stops at the first failure. A cancellation means a newer edit superseded this one.
    const executeAll = async (mutations: TimeEntryMutation[], onExecuted?: (mutation: TimeEntryMutation) => void): Promise<ExecuteAllResult> => {
        for (let i = 0; i < mutations.length; i++) {
            const result = await execute(mutations[i]);

            if (result.status === "cancelled") return { status: "cancelled" };

            if (result.status === "error") {
                return {
                    status: "error",
                    error: result.error,
                    validation: result.validation,
                    failedMutation: mutations[i],
                    remaining: mutations.slice(i + 1)
                };
            }
            onExecuted?.(mutations[i]);
        }
        return { status: "success" };
    };

    const executeCreate = async (mutation: TimeEntryCreateMutation) => {
        const result = await timeEntryStore.create(mutation.create);

        if (result.status === "success") {
            if (mutation.event.kind === "draft") {
                removeDraftEvent(mutation.event.uiId);
            } else {
                await suggestionStore.accept(mutation.event.timeEntry.id);
            }
        }

        return result;
    };

    const executeUpdate = async (mutation: TimeEntryUpdateMutation) => {
        if (isExistingUpdateMutation(mutation)) {
            return await timeEntryStore.update(mutation.event.timeEntry.id, mutation.update);
        } else {
            return await suggestionStore.update(mutation.event.timeEntry.id, mutation.update);
        }
    };

    const executeDelete = async (mutation: TimeEntryDeleteMutation) => {
        if (mutation.event.kind === "draft") {
            removeDraftEvent(mutation.event.uiId);
            return success();
        } else if (mutation.event.kind === "existing") {
            return await timeEntryStore.remove(mutation.event.timeEntry.id);
        } else {
            return await suggestionStore.dismiss(mutation.event.timeEntry.id);
        }
    };

    const removeDraftEvent = (uiId: string) => {
        const index = draftEvents.value.findIndex((draft) => draft.uiId === uiId);
        if (index !== -1) draftEvents.value.splice(index, 1);
    };

    return { execute, executeAll, removeDraftEvent };
}
