type DragPoint = {
    x: number;
    y: number;
};

// Lets an overlay be pushed aside with the mouse. The overlay keeps its own
// positioning and is only shifted by an offset, so nothing about how it is
// anchored has to change.
export function useDraggableOverlay() {
    const offset = ref<DragPoint>({ x: 0, y: 0 });
    const isDragging = ref(false);

    let grabbedAt: DragPoint = { x: 0, y: 0 };
    let offsetAtGrab: DragPoint = { x: 0, y: 0 };

    const start = (nativeEvent: MouseEvent): void => {
        if (nativeEvent.button !== 0) return;
        nativeEvent.preventDefault();

        isDragging.value = true;
        grabbedAt = { x: nativeEvent.clientX, y: nativeEvent.clientY };
        offsetAtGrab = { ...offset.value };
    };

    const update = (nativeEvent: MouseEvent): void => {
        if (!isDragging.value) return;

        offset.value = {
            x: offsetAtGrab.x + nativeEvent.clientX - grabbedAt.x,
            y: offsetAtGrab.y + nativeEvent.clientY - grabbedAt.y
        };
    };

    const stop = (): void => {
        isDragging.value = false;
    };

    const reset = (): void => {
        offset.value = { x: 0, y: 0 };
    };

    useEventListener(window, "mousemove", update);
    useEventListener(window, "mouseup", stop);

    // Deliberately the standalone translate property and not transform: the
    // overlays animate their own transform, and this offset has to compose with
    // that instead of replacing it.
    const style = computed(() => {
        const { x, y } = offset.value;
        if (x === 0 && y === 0) return undefined;

        return { translate: `${x}px ${y}px` };
    });

    return { offset, isDragging, style, start, stop, reset };
}
