import { isDraftAdd, type DraftTimeEntryEvent, type Gesture, type StagedChange, type Task, type TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useEventWrapper } from "@/components/tracking/calendar/composables/useEventWrapper";

export const useCalendarStore = defineStore("calendar", () => {
    const timeEntryStore = useTimeEntryStore();
    const suggestionStore = useTimeEntrySuggestionStore();
    const { createExistingEvent, createSuggestionEvent } = useEventWrapper();

    const gesture = ref<Gesture>({ kind: "idle" });
    const task = ref<Task>({ kind: "none" });

    const stagedChanges = ref<Map<string, StagedChange>>(new Map());

    const existingEvents = computed(() => timeEntryStore.timeEntries.map((c) => createExistingEvent(c)));
    const suggestionEvents = computed(() => suggestionStore.timeEntrySuggestions.map((c) => createSuggestionEvent(c)));

    // A draft is a pending create and nothing else. Sorted, because map order shifts on revert.
    const draftEvents = computed<readonly DraftTimeEntryEvent[]>(() => {
        const changes = [...stagedChanges.value.values()];
        const drafts = changes.filter(isDraftAdd).map((change) => change.event);

        return drafts.sort((a, b) => a.start - b.start);
    });

    // The one being drawn belongs to the gesture: it is not a pending create yet.
    const gestureDraft = computed<DraftTimeEntryEvent[]>(() => {
        if (gesture.value.kind !== "draft") return [];
        return [gesture.value.event];
    });

    const events = computed<TimeEntryEvent[]>(() => [...existingEvents.value, ...suggestionEvents.value, ...draftEvents.value, ...gestureDraft.value]);

    // Counted, not a flag: a superseded commit can still be unwinding.
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
        gestureDraft,
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
