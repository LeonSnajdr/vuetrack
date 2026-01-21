import type { TimeEntryMutation } from "@/components/tracking/calendar/types";
import { useEventMutation } from "./useEventMutation";

export function useConflict() {
    const calendarStore = useCalendarStore();
    const { interaction, conflictLoadingId } = storeToRefs(calendarStore);
    const mutation = useEventMutation();

    const finish = async (mutations: TimeEntryMutation[]) => {
        if (interaction.value.kind !== "conflict") return;

        // Execute all mutations
        await mutation.executeAll(mutations);

        conflictLoadingId.value = null;
        interaction.value = { kind: "idle" };
    };

    const cancel = async () => {
        if (interaction.value.kind !== "conflict") return;
        const { event, mutation: conflictMutation } = interaction.value;

        // Rollback to original position from mutation
        event.start = conflictMutation.originalPosition.start;
        event.end = conflictMutation.originalPosition.end;

        conflictLoadingId.value = null;
        interaction.value = { kind: "idle" };
    };

    return { finish, cancel };
}
