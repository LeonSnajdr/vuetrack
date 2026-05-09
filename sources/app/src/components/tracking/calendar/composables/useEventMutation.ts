import { success } from "@/util/ActionResult";
import { ApiValidationException, type ApiValidationError } from "@/util/ApiValidationError";
import type {
    DraftTimeEntryCreateMutation,
    DraftTimeEntryDeleteMutation,
    ExistingTimeEntryDeleteMutation,
    ExistingTimeEntryUpdateMutation,
    Interaction,
    SuggestionTimeEntryCreateMutation,
    SuggestionTimeEntryDeleteMutation,
    SuggestionTimeEntryUpdateMutation,
    TimeEntryEvent,
    TimeEntryMutation
} from "@/components/tracking/calendar/types";
import { useCalendarHelper } from "./useCalendarHelper";

type ConflictMutation =
    | ExistingTimeEntryUpdateMutation
    | SuggestionTimeEntryUpdateMutation
    | DraftTimeEntryCreateMutation
    | SuggestionTimeEntryCreateMutation;

export type ExecuteAllResult =
    | { status: "success" }
    | {
          status: "error";
          error?: unknown;
          failedMutation: TimeEntryMutation;
          remaining: TimeEntryMutation[];
      };

export function buildHandoffInteraction(failedMutation: TimeEntryMutation, remaining: TimeEntryMutation[], errors: ApiValidationError): Interaction | null {
    if (failedMutation.kind === "update") {
        return {
            kind: "edit",
            event: failedMutation.event,
            mutation: failedMutation,
            errors,
            pendingMutations: remaining
        };
    }
    if (failedMutation.kind === "create") {
        return {
            kind: "create",
            event: failedMutation.event,
            mutation: failedMutation,
            errors,
            pendingMutations: remaining
        };
    }
    return null;
}

export function useEventMutation() {
    const calendarStore = useCalendarStore();
    const timeEntryStore = useTimeEntryStore();
    const suggestionStore = useTimeEntrySuggestionStore();
    const { getOverlappingEvents, buildDeleteMutation, restoreOriginalPosition } = useCalendarHelper();

    const { draftEvents, existingEvents, interaction } = storeToRefs(calendarStore);

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

    const executeAll = async (mutations: TimeEntryMutation[]): Promise<ExecuteAllResult> => {
        for (let i = 0; i < mutations.length; i++) {
            const result = await execute(mutations[i]);
            if (result.status !== "success") {
                return {
                    status: "error",
                    error: result.status === "error" ? result.error : undefined,
                    failedMutation: mutations[i],
                    remaining: mutations.slice(i + 1)
                };
            }
        }
        return { status: "success" };
    };

    const executeCreate = async (mutation: DraftTimeEntryCreateMutation | SuggestionTimeEntryCreateMutation) => {
        const result = await timeEntryStore.create(mutation.create);

        if (result.status === "success") {
            if (mutation.event.kind === "draft") {
                const idx = draftEvents.value.indexOf(mutation.event);
                if (idx !== -1) draftEvents.value.splice(idx, 1);
            } else {
                await suggestionStore.dismiss(mutation.event.timeEntry.id);
            }
        }

        return result;
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

    // If `event` overlaps any existing event, switch the interaction to
    // "conflict" and return true so the caller can short-circuit.
    const tryEnterConflict = (event: TimeEntryEvent, mut: ConflictMutation): boolean => {
        const overlaps = getOverlappingEvents(event, existingEvents.value);
        if (overlaps.length === 0) return false;
        interaction.value = { kind: "conflict", event, overlaps, mutation: mut };
        return true;
    };

    // Drains the pending-mutation queue. Returns true when the caller should
    // proceed (set interaction to idle); returns false when this function has
    // already taken over the interaction state via a handoff.
    const drainPending = async (pendingMutations: TimeEntryMutation[]): Promise<boolean> => {
        const result = await executeAll(pendingMutations);
        if (result.status === "success") return true;

        if (result.error instanceof ApiValidationException) {
            const handoff = buildHandoffInteraction(result.failedMutation, result.remaining, result.error.errors);
            if (handoff) {
                interaction.value = handoff;
                return false;
            }
        }

        // Failure with no recoverable handoff (delete validation, network etc.):
        // roll back optimistic UI changes from mutations that never ran.
        cancelPending([result.failedMutation, ...result.remaining]);
        return true;
    };

    // Roll back optimistic UI changes from queued conflict-resolution mutations
    // that never ran (because of cancel or an upstream failure). Restores
    // repositioned events and removes any draft events that were going to be
    // created.
    const cancelPending = (pendingMutations: TimeEntryMutation[] | undefined) => {
        if (!pendingMutations) return;
        for (const m of pendingMutations) {
            restoreOriginalPosition(m);
            if (m.kind === "create" && m.event.kind === "draft") {
                execute(buildDeleteMutation(m.event));
            }
        }
    };

    return { execute, executeAll, tryEnterConflict, drainPending, cancelPending };
}
