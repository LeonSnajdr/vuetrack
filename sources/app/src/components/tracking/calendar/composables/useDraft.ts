import { createDraftEvent } from "@/components/tracking/calendar/createEventWrapper";
import { createEditableTimeEntry, roundTime } from "./shared";
import { useEventMutation } from "./useEventMutation";

export function useDraft() {
    const calendarStore = useCalendarStore();
    const mutation = useEventMutation();
    const { interaction, draftEvents, existingEvents, preselectedTaskId } = storeToRefs(calendarStore);

    const start = (anchorMs: number) => {
        const snapPoints = existingEvents.value.flatMap((event) => [event.start, event.end]);
        const anchorStartMs = roundTime(anchorMs, { snapPoints });
        const newEvent = createDraftEvent(anchorStartMs, preselectedTaskId.value.trim());
        draftEvents.value.push(newEvent);

        interaction.value = {
            kind: "draft",
            event: newEvent,
            anchorStartMs
        };
    };

    const update = (mouseMs: number) => {
        if (interaction.value.kind !== "draft") return;
        const { event, anchorStartMs } = interaction.value;
        const down = mouseMs < anchorStartMs;
        const snapPoints = existingEvents.value.flatMap((existingEvent) => [existingEvent.start, existingEvent.end]);
        const mouseRounded = roundTime(mouseMs, { down, snapPoints });

        event.start = Math.min(mouseRounded, anchorStartMs);
        event.end = Math.max(mouseRounded, anchorStartMs);
    };

    const finish = () => {
        if (interaction.value.kind !== "draft") return;
        const cur = interaction.value;
        interaction.value = {
            kind: "create",
            event: cur.event,
            mutation: {
                kind: "create",
                event: cur.event,
                create: createEditableTimeEntry(cur.event.createEntry)
            }
        };
    };

    const cancel = () => {
        if (interaction.value.kind !== "draft") return;
        const cur = interaction.value;
        mutation.execute({ kind: "delete", event: cur.event });
        interaction.value = { kind: "idle" };
    };

    return { start, update, finish, cancel };
}
