import type { DraftTimeEntryEvent, Gesture, StagedChange, Task, TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useEventWrapper } from "@/components/tracking/calendar/composables/useEventWrapper";

export const useCalendarStore = defineStore("calendar", () => {
    const timeEntryStore = useTimeEntryStore();
    const suggestionStore = useTimeEntrySuggestionStore();
    const { createExistingEvent, createSuggestionEvent } = useEventWrapper();

    const existingEvents = computed(() => timeEntryStore.timeEntries.map((c) => createExistingEvent(c)));
    const suggestionEvents = computed(() => suggestionStore.timeEntrySuggestions.map((c) => createSuggestionEvent(c)));
    const draftEvents = ref<DraftTimeEntryEvent[]>([]);
    const events = computed<TimeEntryEvent[]>(() => [...existingEvents.value, ...suggestionEvents.value, ...draftEvents.value]);

    const gesture = ref<Gesture>({ kind: "idle" });
    const task = ref<Task>({ kind: "none" });

    const stagedChanges = ref<Map<string, StagedChange>>(new Map());

    // Counted, not a flag: a superseded commit can still be unwinding while the
    // next one is already running.
    const activeCommits = ref(0);
    const isCommittingChanges = computed(() => activeCommits.value > 0);

    const isLoadingEvents = computed(() => {
        return timeEntryStore.isLoading || suggestionStore.isLoading;
    });

    const isDeletingEvent = computed(() => {
        return timeEntryStore.isDeleting || suggestionStore.isDismissing;
    });

    const isCreatingEvent = computed(() => {
        return timeEntryStore.isCreating;
    });

    const isUpdatingEvent = computed(() => {
        return timeEntryStore.isUpdating || suggestionStore.isUpdating;
    });

    return {
        existingEvents,
        suggestionEvents,
        draftEvents,
        events,
        gesture,
        task,
        stagedChanges,
        activeCommits,
        isCommittingChanges,
        isLoadingEvents,
        isDeletingEvent,
        isCreatingEvent,
        isUpdatingEvent
    };
});
