import type {
    TimeEntryMutation,
    DraftTimeEntryDeleteMutation,
    ExistingTimeEntryDeleteMutation,
    ExistingTimeEntryUpdateMutation,
    SuggestionTimeEntryDeleteMutation,
    SuggestionTimeEntryUpdateMutation
} from "@/components/tracking/calendar/types";
import type { ConflictResolutionResult, EventMutation } from "@/components/tracking/calendar/features/ConflictDialog.vue";
import { useEventMutation } from "./useEventMutation";

export function useConflict() {
    const calendarStore = useCalendarStore();
    const { interaction, conflictLoadingId } = storeToRefs(calendarStore);
    const mutation = useEventMutation();

    const finish = async (result: ConflictResolutionResult) => {
        if (interaction.value.kind !== "conflict") return;

        // Apply side-effect mutations (other events being moved/removed)
        await applyMutations(result.mutations);

        // Update event wrapper to resolved position
        const { event, mutation: conflictMutation } = interaction.value;
        event.start = result.position.start;
        event.end = result.position.end;

        // Execute the main mutation (Date refs already updated via event wrapper)
        await mutation.execute(conflictMutation);

        conflictLoadingId.value = null;
        interaction.value = { kind: "idle" };
    };

    const cancel = async () => {
        if (interaction.value.kind !== "conflict") return;
        const { event, mutation: conflictMutation } = interaction.value;

        // Rollback to original position from mutation
        event.start = conflictMutation.originalPosition.start;
        event.end = conflictMutation.originalPosition.end;

        // Update Date references to match rollback
        if (conflictMutation.kind === "update") {
            conflictMutation.update.startTime.setTime(conflictMutation.originalPosition.start);
            conflictMutation.update.endTime.setTime(conflictMutation.originalPosition.end);
        } else if (conflictMutation.kind === "create") {
            conflictMutation.create.startTime.setTime(conflictMutation.originalPosition.start);
            conflictMutation.create.endTime.setTime(conflictMutation.originalPosition.end);
        }

        conflictLoadingId.value = null;
        interaction.value = { kind: "idle" };
    };

    const applyMutations = async (mutations?: EventMutation[]) => {
        const timeEntryMutations: TimeEntryMutation[] = (mutations ?? []).map((m) => {
            if (m.action === "remove") {
                if (m.event.kind === "draft") {
                    return { kind: "delete", event: m.event } as DraftTimeEntryDeleteMutation;
                } else if (m.event.kind === "existing") {
                    return { kind: "delete", event: m.event, id: m.event.timeEntry.id } as ExistingTimeEntryDeleteMutation;
                } else {
                    return { kind: "delete", event: m.event, id: m.event.timeEntry.id } as SuggestionTimeEntryDeleteMutation;
                }
            } else {
                const startTime = new Date(m.start);
                const endTime = new Date(m.end);

                if (m.event.kind === "existing") {
                    return {
                        kind: "update",
                        event: m.event,
                        update: { startTime, endTime, taskId: m.event.timeEntry.taskId },
                        originalPosition: { start: m.event.start, end: m.event.end }
                    } as ExistingTimeEntryUpdateMutation;
                } else if (m.event.kind === "suggestion") {
                    return {
                        kind: "update",
                        event: m.event,
                        update: { startTime, endTime, taskId: m.event.timeEntry.taskId },
                        originalPosition: { start: m.event.start, end: m.event.end }
                    } as SuggestionTimeEntryUpdateMutation;
                }
                throw new Error("Unexpected event kind for update mutation");
            }
        });

        await mutation.executeAll(timeEntryMutations);
    };

    return { finish, cancel };
}
