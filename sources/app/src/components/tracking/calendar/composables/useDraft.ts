import { useCalendarHelper } from "./useCalendarHelper";
import { useEventMutation } from "./useEventMutation";
import { useEventWrapper } from "./useEventWrapper";

export function useDraft() {
    const calendarStore = useCalendarStore();
    const mutation = useEventMutation();
    const { buildCreateMutation, buildDeleteMutation, roundTime, updateEventPosition } = useCalendarHelper();
    const { createDraftEvent } = useEventWrapper();
    const { interaction, draftEvents, existingEvents } = storeToRefs(calendarStore);

    const start = (anchorMs: number) => {
        const snapPoints = existingEvents.value.flatMap((event) => [event.start, event.end]);
        const anchorStartMs = roundTime(anchorMs, { snapPoints });
        const newEvent = createDraftEvent(anchorStartMs);
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

        updateEventPosition(event, { start: Math.min(mouseRounded, anchorStartMs), end: Math.max(mouseRounded, anchorStartMs) }, down ? "end" : "start");
    };

    const finish = () => {
        if (interaction.value.kind !== "draft") return;
        const { event } = interaction.value;
        interaction.value = {
            kind: "create",
            event,
            mutation: buildCreateMutation(event)
        };
    };

    const cancel = () => {
        if (interaction.value.kind !== "draft") return;
        mutation.execute(buildDeleteMutation(interaction.value.event));
        interaction.value = { kind: "idle" };
    };

    return { start, update, finish, cancel };
}
