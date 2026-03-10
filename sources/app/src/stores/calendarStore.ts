import type { Interaction, DraftTimeEntryEvent, TimeEntryEvent } from "@/components/tracking/calendar/types";
import { createExistingEventWrapper, createSuggestionEventWrapper } from "@/components/tracking/calendar/createEventWrapper";

export const useCalendarStore = defineStore("calendar", () => {
    const timeEntryStore = useTimeEntryStore();
    const suggestionStore = useTimeEntrySuggestionStore();

    const existingEvents = computed(() => timeEntryStore.timeEntries.map((c) => createExistingEventWrapper(c)));
    const suggestionEvents = computed(() => suggestionStore.timeEntrySuggestions.map((c) => createSuggestionEventWrapper(c)));
    const draftEvents = ref<DraftTimeEntryEvent[]>([]);
    const events = computed<TimeEntryEvent[]>(() => [...existingEvents.value, ...suggestionEvents.value, ...draftEvents.value]);

    const interaction = ref<Interaction>({ kind: "idle" });
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
        preselectedTaskId,
        isDeletingEvent,
        isCreatingEvent,
        isUpdatingEvent
    };
});
