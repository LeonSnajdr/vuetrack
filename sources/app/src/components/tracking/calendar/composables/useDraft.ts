import { createDraftEvent } from "@/components/tracking/calendar/createEventWrapper";
import { roundTime } from "./shared";

export function useDraft() {
    const calendarStore = useCalendarStore();
    const { interaction, draftEvents } = storeToRefs(calendarStore);

    const start = (anchorMs: number) => {
        const anchorStartMs = roundTime(anchorMs);
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
        const mouseRounded = roundTime(mouseMs, false);

        event.start = Math.min(mouseRounded, anchorStartMs);
        event.end = Math.max(mouseRounded, anchorStartMs);
    };

    const finish = () => {
        if (interaction.value.kind !== "draft") return;
        const cur = interaction.value;
        interaction.value = { kind: "create", event: cur.event };
    };

    const cancel = () => {
        if (interaction.value.kind !== "draft") return;
        const cur = interaction.value;
        const idx = draftEvents.value.indexOf(cur.event);
        if (idx !== -1) draftEvents.value.splice(idx, 1);
        interaction.value = { kind: "idle" };
    };

    return { start, update, finish, cancel };
}
