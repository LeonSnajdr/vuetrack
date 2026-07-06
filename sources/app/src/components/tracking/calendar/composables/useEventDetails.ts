import type { TimeEntryEvent } from "@/components/tracking/calendar/types";

type DetailsState = {
    show: boolean;
    x: number;
    y: number;
    event: TimeEntryEvent | null;
};

const state = ref<DetailsState>({ show: false, x: 0, y: 0, event: null });

const contextMenuOpen = ref(false);

export function useEventDetails() {
    const calendarStore = useCalendarStore();
    const { interaction } = storeToRefs(calendarStore);

    const open = (nativeEvent: MouseEvent, event: TimeEntryEvent) => {
        if (interaction.value.kind !== "idle") return;
        if (contextMenuOpen.value) return;
        if (event.kind === "draft") return;

        state.value.x = nativeEvent.clientX;
        state.value.y = nativeEvent.clientY;
        state.value.event = event;
        state.value.show = true;
    };

    const move = (nativeEvent: MouseEvent, event: TimeEntryEvent) => {
        if (interaction.value.kind !== "idle" || contextMenuOpen.value) {
            close();
            return;
        }
        
        open(nativeEvent, event);
    };

    const close = () => {
        state.value.show = false;
    };

    const setContextMenuOpen = (open: boolean) => {
        contextMenuOpen.value = open;
        if (open) close();
    };

    return { state, open, move, close, setContextMenuOpen };
}
