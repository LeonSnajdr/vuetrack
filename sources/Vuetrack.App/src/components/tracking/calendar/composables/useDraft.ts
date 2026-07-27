import { useCalendarHelper } from "./useCalendarHelper";
import { useEventMutation } from "./useEventMutation";
import { useEventWrapper } from "./useEventWrapper";

export function useDraft() {
    const calendarStore = useCalendarStore();
    const mutation = useEventMutation();
    const { buildCreatePayload, getAllBoundaries, getEventBoundaries, roundTime, updateEventPosition } = useCalendarHelper();
    const { createDraftEvent } = useEventWrapper();
    const { gesture, task, draftEvents, events } = storeToRefs(calendarStore);

    const start = (anchorMs: number) => {
        const snapPoints = getAllBoundaries(events.value);
        const anchorStartMs = roundTime(anchorMs, { snapPoints });
        const newEvent = createDraftEvent(anchorStartMs);
        draftEvents.value.push(newEvent);

        gesture.value = {
            kind: "draft",
            event: newEvent,
            anchorStartMs
        };
    };

    const update = (mouseMs: number) => {
        if (gesture.value.kind !== "draft") return;
        const { event, anchorStartMs } = gesture.value;
        const down = mouseMs < anchorStartMs;
        const snapPoints = getEventBoundaries(event, events.value);
        const mouseRounded = roundTime(mouseMs, { down, snapPoints });

        updateEventPosition(event, { start: Math.min(mouseRounded, anchorStartMs), end: Math.max(mouseRounded, anchorStartMs) }, down ? "end" : "start");
    };

    const finish = () => {
        if (gesture.value.kind !== "draft") return;

        const { event } = gesture.value;
        gesture.value = { kind: "idle" };

        const payload = buildCreatePayload(event);
        task.value = { kind: "create", event, payload };
    };

    const cancel = () => {
        if (gesture.value.kind !== "draft") return;

        const { event } = gesture.value;
        gesture.value = { kind: "idle" };

        mutation.removeDraftEvent(event.uiId);
    };

    return { start, update, finish, cancel };
}
