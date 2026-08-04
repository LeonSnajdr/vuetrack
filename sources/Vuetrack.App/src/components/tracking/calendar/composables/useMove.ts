import type { TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useCalendarHelper } from "./useCalendarHelper";
import { useChangeSet } from "./useChangeSet";
import { useEventCommit } from "./useEventCommit";

export function useMove() {
    const calendarStore = useCalendarStore();
    const changeSet = useChangeSet();
    const commit = useEventCommit();
    const { roundTime, getEventBoundaries, cancelPendingUpdateForEvent, minimumEventDurationMs, clampPosition } = useCalendarHelper();

    const { gesture, events } = storeToRefs(calendarStore);

    const start = (event: TimeEntryEvent) => {
        cancelPendingUpdateForEvent(event);

        gesture.value = {
            kind: "move",
            event,
            from: { start: event.start, end: event.end },
            wasStaged: changeSet.has(event.uiId),
            pointerOffsetMs: undefined
        };
    };

    const setPointerOffset = (mouseMs: number) => {
        if (gesture.value.kind !== "move") return;
        if (gesture.value.pointerOffsetMs !== undefined) return;
        gesture.value.pointerOffsetMs = mouseMs - gesture.value.event.start;
    };

    const update = (mouseMs: number) => {
        if (gesture.value.kind !== "move") return;
        const { event, pointerOffsetMs } = gesture.value;
        if (pointerOffsetMs === undefined) return;

        const duration = Math.max(event.end - event.start, minimumEventDurationMs);
        const snapPoints = getEventBoundaries(event, events.value).flatMap((boundary) => [boundary, boundary - duration]);
        const newStart = roundTime(mouseMs - pointerOffsetMs, { snapPoints });
        const position = clampPosition({ start: newStart, end: newStart + duration }, "start");

        changeSet.stagePosition(event, position);
    };

    const finish = async () => {
        if (gesture.value.kind !== "move") return;

        const { event } = gesture.value;
        gesture.value = { kind: "idle" };

        await commit.commitGesture(event);
    };

    const cancel = () => {
        if (gesture.value.kind !== "move") return;

        const { event, from, wasStaged } = gesture.value;
        gesture.value = { kind: "idle" };

        if (!wasStaged) {
            changeSet.unstage(event.uiId);
            return;
        }

        changeSet.stagePosition(event, from);
    };

    return { start, setPointerOffset, update, finish, cancel };
}
