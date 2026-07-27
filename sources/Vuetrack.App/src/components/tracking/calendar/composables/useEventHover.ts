import type { TimeEntryEvent } from "@/components/tracking/calendar/types";

const hoveredUiId = ref<string | null>(null);

export function useEventHover() {
    const calendarStore = useCalendarStore();
    const { events } = storeToRefs(calendarStore);

    const hoveredEvent = computed<TimeEntryEvent | null>(() => {
        if (!hoveredUiId.value) return null;
        const event = events.value.find((candidate) => candidate.uiId === hoveredUiId.value);
        return event ?? null;
    });

    const setHovered = (event: TimeEntryEvent) => {
        hoveredUiId.value = event.uiId;
    };

    const clearHovered = (event: TimeEntryEvent) => {
        if (hoveredUiId.value !== event.uiId) return;
        hoveredUiId.value = null;
    };

    return { hoveredEvent, setHovered, clearHovered };
}
