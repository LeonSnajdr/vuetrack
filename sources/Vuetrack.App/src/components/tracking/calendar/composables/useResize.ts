import type { EventEdge, TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useCalendarHelper } from "./useCalendarHelper";
import { useChangeSet } from "./useChangeSet";
import { useEventCommit } from "./useEventCommit";

export function useResize() {
    const calendarStore = useCalendarStore();
    const changeSet = useChangeSet();
    const commit = useEventCommit();
    const { roundTime, getEventBoundaries, cancelPendingUpdateForEvent, updateEventPosition, applyEventPosition } = useCalendarHelper();

    const { gesture, events } = storeToRefs(calendarStore);

    const start = (event: TimeEntryEvent, edge: EventEdge = "end") => {
        cancelPendingUpdateForEvent(event);
        if (event.kind !== "draft") changeSet.stageUpdate(event);

        gesture.value = {
            kind: "resize",
            edge,
            event,
            from: { start: event.start, end: event.end }
        };
    };

    const update = (mouseMs: number) => {
        if (gesture.value.kind !== "resize") return;

        const { event, edge } = gesture.value;
        const snapPoints = getEventBoundaries(event, events.value);

        if (edge === "start") {
            const mouseRounded = roundTime(mouseMs, { down: true, snapPoints });
            updateEventPosition(event, { start: mouseRounded }, "end");
        } else {
            const mouseRounded = roundTime(mouseMs, { down: false, snapPoints });
            updateEventPosition(event, { end: mouseRounded }, "start");
        }
    };

    const finish = async () => {
        if (gesture.value.kind !== "resize") return;

        const { event } = gesture.value;
        gesture.value = { kind: "idle" };

        await commit.commitGesture(event);
    };

    const cancel = () => {
        if (gesture.value.kind !== "resize") return;

        const { event, from } = gesture.value;
        gesture.value = { kind: "idle" };

        applyEventPosition(event, from.start, from.end);
        changeSet.unstageIfUnchanged(event.uiId);
    };

    return { start, update, finish, cancel };
}
