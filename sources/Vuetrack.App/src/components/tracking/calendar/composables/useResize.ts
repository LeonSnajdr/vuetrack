import type { EventEdge, TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useCalendarHelper } from "./useCalendarHelper";
import { useChangeSet } from "./useChangeSet";
import { useEventCommit } from "./useEventCommit";

export function useResize() {
    const calendarStore = useCalendarStore();
    const changeSet = useChangeSet();
    const commit = useEventCommit();
    const { roundTime, getEventBoundaries, cancelPendingUpdateForEvent, clampPosition } = useCalendarHelper();

    const { gesture, events } = storeToRefs(calendarStore);

    const start = (event: TimeEntryEvent, edge: EventEdge = "end") => {
        cancelPendingUpdateForEvent(event);

        gesture.value = {
            kind: "resize",
            edge,
            event,
            from: { start: event.start, end: event.end },
            wasStaged: changeSet.has(event.uiId)
        };
    };

    const update = (mouseMs: number) => {
        if (gesture.value.kind !== "resize") return;

        const { event, edge } = gesture.value;
        const snapPoints = getEventBoundaries(event, events.value);

        if (edge === "start") {
            const mouseRounded = roundTime(mouseMs, { down: true, snapPoints });
            const position = clampPosition({ start: mouseRounded, end: event.end }, "end");
            changeSet.stagePosition(event, position);
            return;
        }

        const mouseRounded = roundTime(mouseMs, { down: false, snapPoints });
        const position = clampPosition({ start: event.start, end: mouseRounded }, "start");
        changeSet.stagePosition(event, position);
    };

    const finish = async () => {
        if (gesture.value.kind !== "resize") return;

        const { event } = gesture.value;
        gesture.value = { kind: "idle" };

        await commit.commitGesture(event);
    };

    const cancel = () => {
        if (gesture.value.kind !== "resize") return;

        const { event, from, wasStaged } = gesture.value;
        gesture.value = { kind: "idle" };

        if (!wasStaged) {
            changeSet.unstage(event.uiId);
            return;
        }

        changeSet.stagePosition(event, from);
    };

    return { start, update, finish, cancel };
}
