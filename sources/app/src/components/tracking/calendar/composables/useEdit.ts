import type { ExistingTimeEntryEvent, SuggestionTimeEntryEvent } from "@/components/tracking/calendar/types";
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
                          get startTime() {
                              return event.timeEntry.startTime;
                          },
                          get endTime() {
                              return event.timeEntry.endTime;
                          },
                          taskId: event.timeEntry.taskId
                      },
                      originalPosition: { start: event.start, end: event.end }
                  }
                : {
                      kind: "update" as const,
                      event,
                      update: {
                          get startTime() {
                              return event.timeEntry.startTime;
                          },
                          get endTime() {
                              return event.timeEntry.endTime;
                          },
                          taskId: event.timeEntry.taskId
                      },
                      originalPosition: { start: event.start, end: event.end }
                  };

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
