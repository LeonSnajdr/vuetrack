import type { ExistingTimeEntryEvent, SuggestionTimeEntryEvent, TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useEventMutation } from "./useEventMutation";

export function useEdit() {
    const calendarStore = useCalendarStore();
    const { interaction, editLoading } = storeToRefs(calendarStore);
    const mutation = useEventMutation();

    const start = (event: ExistingTimeEntryEvent | SuggestionTimeEntryEvent) => {
        const editMutation =
            event.kind === "existing"
                ? {
                      kind: "update" as const,
                      event,
                      update: {
                          startTime: event.timeEntry.startTime,
                          endTime: event.timeEntry.endTime,
                          taskId: event.timeEntry.taskId
                      },
                      originalPosition: { start: event.start, end: event.end }
                  }
                : {
                      kind: "update" as const,
                      event,
                      update: {
                          startTime: event.timeEntry.startTime,
                          endTime: event.timeEntry.endTime,
                          taskId: event.timeEntry.taskId
                      },
                      originalPosition: { start: event.start, end: event.end }
                  };

        interaction.value = { kind: "edit", event, mutation: editMutation };
    };

    const finish = async (event: TimeEntryEvent) => {
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
