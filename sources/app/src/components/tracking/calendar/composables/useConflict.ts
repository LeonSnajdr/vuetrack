import type { TimeEntryMutation } from "@/components/tracking/calendar/types";
import { useEventMutation } from "./useEventMutation";

export function useConflict() {
    const calendarStore = useCalendarStore();
    const { interaction } = storeToRefs(calendarStore);
    const mutation = useEventMutation();

    const finish = async (mutations: TimeEntryMutation[]) => {
        if (interaction.value.kind !== "conflict") return;

        await mutation.executeAll(mutations);

        interaction.value = { kind: "idle" };
    };

    const cancel = async () => {
        if (interaction.value.kind !== "conflict") return;
        const { event, mutation: conflictMutation } = interaction.value;

        if (event.kind === "draft") {
            mutation.execute({ kind: "delete", event });
        }

        if ("originalPosition" in conflictMutation) {
            event.start = conflictMutation.originalPosition.start;
            event.end = conflictMutation.originalPosition.end;
        }

        interaction.value = { kind: "idle" };
    };

    return { finish, cancel };
}
