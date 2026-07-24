import type { TimeEntryMutation } from "@/components/tracking/calendar/types";
import { useCalendarHelper } from "./useCalendarHelper";
import { useEventMutation } from "./useEventMutation";

export function useConflict() {
    const calendarStore = useCalendarStore();
    const { interaction } = storeToRefs(calendarStore);
    const mutation = useEventMutation();
    const { restoreOriginalPosition } = useCalendarHelper();

    const finish = async (mutations: TimeEntryMutation[]) => {
        if (interaction.value.kind !== "conflict") return;

        const shouldIdle = await mutation.drainPending(mutations);
        if (shouldIdle) {
            interaction.value = { kind: "idle" };
        }
    };

    const cancel = async () => {
        if (interaction.value.kind !== "conflict") return;
        const { event, mutation: conflictMutation } = interaction.value;

        mutation.deleteIfDraft(event);
        restoreOriginalPosition(conflictMutation);

        interaction.value = { kind: "idle" };
    };

    return { finish, cancel };
}
