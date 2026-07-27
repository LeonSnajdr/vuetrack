import type { TimeEntryEvent } from "@/components/tracking/calendar/types";

type DetailsState = {
    show: boolean;
    pinned: boolean;
    x: number;
    y: number;
    event: TimeEntryEvent | null;
};

const state = ref<DetailsState>({ show: false, pinned: false, x: 0, y: 0, event: null });

const contextMenuOpen = ref(false);

watch(
    () => state.value.show,
    (show) => {
        if (!show) state.value.pinned = false;
    }
);

export function useEventDetails() {
    const calendarStore = useCalendarStore();
    const { gesture, task } = storeToRefs(calendarStore);

    const isBusy = computed(() => gesture.value.kind !== "idle" || task.value.kind !== "none");

    const open = (nativeEvent: MouseEvent, event: TimeEntryEvent) => {
        if (state.value.pinned) return;
        if (isBusy.value) return;
        if (contextMenuOpen.value) return;
        if (event.kind === "draft") return;

        state.value.x = nativeEvent.clientX;
        state.value.y = nativeEvent.clientY;
        state.value.event = event;
        state.value.show = true;
    };

    const move = (nativeEvent: MouseEvent, event: TimeEntryEvent) => {
        if (state.value.pinned) return;

        if (isBusy.value || contextMenuOpen.value) {
            close();
            return;
        }

        open(nativeEvent, event);
    };

    const close = () => {
        if (state.value.pinned) return;
        state.value.show = false;
    };

    const hardClose = () => {
        state.value.show = false;
    };

    const togglePin = () => {
        if (state.value.pinned) {
            hardClose();
            return;
        }

        if (!state.value.show || !state.value.event) return;
        state.value.pinned = true;
    };

    const setContextMenuOpen = (open: boolean) => {
        contextMenuOpen.value = open;
        if (open) hardClose();
    };

    return { state, isBusy, open, move, close, hardClose, togglePin, setContextMenuOpen };
}
