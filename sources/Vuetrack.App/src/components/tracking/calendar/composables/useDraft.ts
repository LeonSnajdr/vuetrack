import { useCalendarHelper } from "./useCalendarHelper";
import { useChangeSet } from "./useChangeSet";
import { useEventWrapper } from "./useEventWrapper";

export function useDraft() {
    const calendarStore = useCalendarStore();
    const changeSet = useChangeSet();
    const { getAllBoundaries, getEventBoundaries, roundTime, updateEventPosition } = useCalendarHelper();
    const { createDraftEvent } = useEventWrapper();
    const { gesture, task, events } = storeToRefs(calendarStore);

    const start = (anchorMs: number) => {
        const snapPoints = getAllBoundaries(events.value);
        const anchorStartMs = roundTime(anchorMs, { snapPoints });
        const newEvent = createDraftEvent(anchorStartMs);

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

    // The drawn box turns into a pending create; the overlay edits that same payload.
    const finish = () => {
        if (gesture.value.kind !== "draft") return;

        const { event } = gesture.value;
        const payload = changeSet.stageDraft(event);
        gesture.value = { kind: "idle" };

        task.value = { kind: "create", event, payload };
    };

    const cancel = () => {
        if (gesture.value.kind !== "draft") return;
        gesture.value = { kind: "idle" };
    };

    return { start, update, finish, cancel };
}
