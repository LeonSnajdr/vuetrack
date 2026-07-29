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
    const timeEntryStore = useTimeEntryStore();
    const suggestionStore = useTimeEntrySuggestionStore();

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

    // A draft needs no follow up: unstaging the create is what makes it go away.
    const executeCreate = async (mutation: TimeEntryCreateMutation) => {
        const result = await timeEntryStore.create(mutation.create);

        if (result.status === "success" && mutation.event.kind === "suggestion") {
            await suggestionStore.accept(mutation.event.timeEntry.id);
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
        if (mutation.event.kind === "existing") {
            return await timeEntryStore.remove(mutation.event.timeEntry.id);
        } else {
            return await suggestionStore.dismiss(mutation.event.timeEntry.id);
        }
    };

    return { execute, executeAll };
}
