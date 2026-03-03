import type {
    DraftTimeEntryDeleteMutation,
    ExistingTimeEntryDeleteMutation,
    SuggestionTimeEntryDeleteMutation,
    TimeEntryEvent
} from "@/components/tracking/calendar/types";
import { useEventMutation } from "./useEventMutation";

export function useDelete() {
    const calendarStore = useCalendarStore();
    const mutation = useEventMutation();
    const { interaction } = storeToRefs(calendarStore);

    const start = (event: TimeEntryEvent) => {
        let deleteMutation: DraftTimeEntryDeleteMutation | ExistingTimeEntryDeleteMutation | SuggestionTimeEntryDeleteMutation;
        if (event.kind === "draft") {
            deleteMutation = { kind: "delete", event };
        } else if (event.kind === "existing") {
            deleteMutation = { kind: "delete", event, id: event.timeEntry.id };
        } else {
            deleteMutation = { kind: "delete", event, id: event.timeEntry.id };
        }

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
