import type { TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useCalendarHelper } from "./useCalendarHelper";
import { useEventMutation } from "./useEventMutation";

export function useDelete() {
    const calendarStore = useCalendarStore();
    const mutation = useEventMutation();
    const { buildDeleteMutation } = useCalendarHelper();
    const { interaction } = storeToRefs(calendarStore);

    const start = (event: TimeEntryEvent) => {
        const deleteMutation = buildDeleteMutation(event);
        interaction.value = { kind: "delete", event, mutation: deleteMutation };
    };

    const finish = async () => {
        if (interaction.value.kind !== "delete") return;
        const cur = interaction.value;

        await mutation.execute(cur.mutation);
        interaction.value = { kind: "idle" };
    };

    const cancel = () => {
        if (interaction.value.kind !== "delete") return;
        interaction.value = { kind: "idle" };
    };

    return { start, finish, cancel };
}
