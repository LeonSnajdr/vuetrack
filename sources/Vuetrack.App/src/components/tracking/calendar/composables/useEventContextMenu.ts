import type { CalendarEvent } from "vuetify/lib/components/VCalendar/types.mjs";
import { canStartInteraction, type ExistingTimeEntryEvent, type SuggestionTimeEntryEvent } from "@/components/tracking/calendar/types";
import { useCalendarTimePeriod } from "./useCalendarTimePeriod";
import { useEventDetails } from "./useEventDetails";

export type ContextMenuEvent = ExistingTimeEntryEvent | SuggestionTimeEntryEvent;

type ContextMenuState = {
    show: boolean;
    x: number;
    y: number;
    event: ContextMenuEvent | null;
};

const state = ref<ContextMenuState>({ show: false, x: 0, y: 0, event: null });

export function useEventContextMenu() {
    const calendarStore = useCalendarStore();
    const { interaction } = storeToRefs(calendarStore);
    const { isReadonly } = useCalendarTimePeriod();
    const { setContextMenuOpen } = useEventDetails();

    const open = (nativeEvent: Event, event?: CalendarEvent) => {
        const mouseEvent = nativeEvent as MouseEvent;
        mouseEvent.preventDefault();

        if (isReadonly.value) return;
        if (!event) return;
        if (!canStartInteraction(interaction.value.kind)) return;
        if (event.kind !== "existing" && event.kind !== "suggestion") return;

        const target = event as ContextMenuEvent;

        setContextMenuOpen(true);
        state.value = { show: true, x: mouseEvent.clientX, y: mouseEvent.clientY, event: target };
    };

    const close = () => {
        state.value.show = false;
    };

    return { state, open, close };
}
