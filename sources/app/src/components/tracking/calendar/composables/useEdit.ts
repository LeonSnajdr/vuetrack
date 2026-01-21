import type {
    ExistingTimeEntryEvent,
    ExistingTimeEntryUpdateMutation,
    SuggestionTimeEntryEvent,
    SuggestionTimeEntryUpdateMutation
} from "@/components/tracking/calendar/types";
import { useEventMutation } from "./useEventMutation";

export function useEdit() {
    const calendarStore = useCalendarStore();
    const { interaction, editLoading } = storeToRefs(calendarStore);
    const mutation = useEventMutation();

    const start = (event: ExistingTimeEntryEvent | SuggestionTimeEntryEvent) => {
        let editMutation: ExistingTimeEntryUpdateMutation | SuggestionTimeEntryUpdateMutation;
        if (event.kind === "existing") {
            editMutation = {
                kind: "update",
                event,
                update: withProxy({ taskId: event.timeEntry.taskId }).from(event.timeEntry, "startTime", "endTime").build(),
                originalPosition: { start: event.start, end: event.end }
            };
        } else {
            editMutation = {
                kind: "update",
                event,
                update: withProxy({ taskId: event.timeEntry.taskId }).from(event.timeEntry, "startTime", "endTime").build(),
                originalPosition: { start: event.start, end: event.end }
            };
        }

        interaction.value = { kind: "edit", event, mutation: editMutation };
    };

    const finish = async () => {
        if (interaction.value.kind !== "edit") return;

        editLoading.value = true;

        const { mutation: editMutation } = interaction.value;

        await mutation.execute(editMutation);

        editLoading.value = false;
        interaction.value = { kind: "idle" };
    };

    const cancel = () => {
        interaction.value = { kind: "idle" };
    };

    return { start, finish, cancel };
}
