import type { TimeEntryEvent } from "@/components/tracking/calendar/types";

const selectedUiId = ref<string | null>(null);

// Shortcuts act on this, not on whatever the pointer rests on.
export function useEventSelection() {
    const calendarStore = useCalendarStore();
    const { events } = storeToRefs(calendarStore);

    const selectedEvent = computed<TimeEntryEvent | null>(() => {
        if (!selectedUiId.value) return null;

        const event = events.value.find((candidate) => candidate.uiId === selectedUiId.value);
        return event ?? null;
    });

    const select = (event: TimeEntryEvent) => {
        selectedUiId.value = event.uiId;
    };

    const clearSelection = () => {
        selectedUiId.value = null;
    };

    return { selectedUiId, selectedEvent, select, clearSelection };
}
