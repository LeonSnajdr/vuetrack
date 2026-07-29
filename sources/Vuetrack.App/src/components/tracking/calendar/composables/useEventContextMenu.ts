import type { CalendarEvent } from "vuetify/lib/components/VCalendar/types.mjs";
import { isTimeEntryEvent, type TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useCalendarTimePeriod } from "./useCalendarTimePeriod";
import { useEventDetails } from "./useEventDetails";
import { useEventPolicy } from "./useEventPolicy";
import { useEventSelection } from "./useEventSelection";

export type ContextMenuEvent = TimeEntryEvent;

type ContextMenuState = {
    show: boolean;
    x: number;
    y: number;
    event: ContextMenuEvent | null;
};

const state = ref<ContextMenuState>({ show: false, x: 0, y: 0, event: null });

export function useEventContextMenu() {
    const { isReadonly } = useCalendarTimePeriod();
    const { setContextMenuOpen } = useEventDetails();
    const policy = useEventPolicy();
    const { select } = useEventSelection();

    const open = (nativeEvent: Event, event?: CalendarEvent) => {
        const mouseEvent = nativeEvent as MouseEvent;
        mouseEvent.preventDefault();

        if (isReadonly.value) return;
        if (!event) return;
        if (!isTimeEntryEvent(event)) return;
        if (!policy.isSaved(event) && !policy.canStageRemoval(event)) return;
        if (!policy.canOpenTask() && !policy.canStageRemoval(event)) return;

        const target = event;

        select(target);
        setContextMenuOpen(true);
        state.value = { show: true, x: mouseEvent.clientX, y: mouseEvent.clientY, event: target };
    };

    const close = () => {
        state.value.show = false;
    };

    return { state, open, close };
}
