import type { Interaction, DraftTimeEntryEvent, TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useEventWrapper } from "@/components/tracking/calendar/composables/useEventWrapper";

export const useCalendarStore = defineStore("calendar", () => {
    const timeEntryStore = useTimeEntryStore();
    const suggestionStore = useTimeEntrySuggestionStore();
    const { createExistingEvent, createSuggestionEvent } = useEventWrapper();

    const existingEvents = computed(() => timeEntryStore.timeEntries.map((c) => createExistingEvent(c)));
    const suggestionEvents = computed(() => suggestionStore.timeEntrySuggestions.map((c) => createSuggestionEvent(c)));
    const draftEvents = ref<DraftTimeEntryEvent[]>([]);
    const events = computed<TimeEntryEvent[]>(() => [...existingEvents.value, ...suggestionEvents.value, ...draftEvents.value]);

    const interaction = ref<Interaction>({ kind: "idle" });
    const intervalMinutes = ref<number>(30);
    const preselectedTaskId = ref("");

    const isDeletingEvent = computed(() => {
        return timeEntryStore.isDeleting() || suggestionStore.isDismissing();
    });

    const isCreatingEvent = computed(() => {
        return timeEntryStore.isCreating();
    });

    const isUpdatingEvent = computed(() => {
        return timeEntryStore.isUpdating() || suggestionStore.isUpdating();
    });

    return {
        existingEvents,
        suggestionEvents,
        draftEvents,
        events,
        interaction,
        intervalMinutes,
        preselectedTaskId,
        isDeletingEvent,
        isCreatingEvent,
        isUpdatingEvent
    };
});
