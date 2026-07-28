import type { EventEdge, TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useMove } from "./useMove";
import { useResize } from "./useResize";

export type ArmedGestureIntent = { kind: "move"; event: TimeEntryEvent } | { kind: "resize"; event: TimeEntryEvent; edge: EventEdge };

type ArmedGesture = ArmedGestureIntent & {
    anchorClientX: number;
    anchorClientY: number;
    anchorMouseMs?: number;
};

const dragThresholdPx = 4;

const armed = ref<ArmedGesture | null>(null);

// A press is only an intent to drag: the gesture starts once the pointer travelled,
// so a plain click neither re-times the event nor saves it.
export function useGestureArm() {
    const move = useMove();
    const resize = useResize();

    const isArmed = computed(() => armed.value !== null);

    const armGesture = (intent: ArmedGestureIntent, nativeEvent: MouseEvent): void => {
        armed.value = { ...intent, anchorClientX: nativeEvent.clientX, anchorClientY: nativeEvent.clientY };
    };

    // Where inside the event the pointer grabbed it.
    const setAnchorTime = (mouseMs: number): void => {
        if (!armed.value) return;
        armed.value.anchorMouseMs = mouseMs;
    };

    const clearArm = (): void => {
        armed.value = null;
    };

    const hasClearedThreshold = (nativeEvent: MouseEvent, current: ArmedGesture): boolean => {
        const travelX = Math.abs(nativeEvent.clientX - current.anchorClientX);
        const travelY = Math.abs(nativeEvent.clientY - current.anchorClientY);

        return travelX >= dragThresholdPx || travelY >= dragThresholdPx;
    };

    // The anchor keeps the event under the pointer instead of jumping to its rounded time.
    const promoteArm = (nativeEvent: MouseEvent, mouseMs: number): boolean => {
        const current = armed.value;
        if (!current) return false;

        // The button went up unnoticed, so hovering must not become a drag.
        if (nativeEvent.buttons === 0) {
            clearArm();
            return false;
        }

        if (!hasClearedThreshold(nativeEvent, current)) return false;

        clearArm();

        if (current.kind === "resize") {
            resize.start(current.event, current.edge);
            return true;
        }

        move.start(current.event);
        move.setPointerOffset(current.anchorMouseMs ?? mouseMs);
        return true;
    };

    return { isArmed, armGesture, setAnchorTime, clearArm, promoteArm };
}
