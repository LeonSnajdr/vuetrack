import {
    getDraftEvent,
    getPayloadPosition,
    type DraftTimeEntryEvent,
    type EventPosition,
    type Gesture,
    type StagedChange,
    type Task,
    type TimeEntryEvent,
    type UiId
} from "@/components/tracking/calendar/types";
import { useEventWrapper } from "@/components/tracking/calendar/composables/useEventWrapper";

export const useCalendarStore = defineStore("calendar", () => {
    const timeEntryStore = useTimeEntryStore();
    const suggestionStore = useTimeEntrySuggestionStore();
    const { filter } = useTrackingFilter();

    const gesture = ref<Gesture>({ kind: "idle" });
    const task = ref<Task>({ kind: "none" });

    const stagedChanges = ref<Map<UiId, StagedChange>>(new Map());

    // What an event renders at: the staged proposal if there is one, else what is saved.
    const resolveStagedPosition = (uiId: UiId): EventPosition | null => {
        const change = stagedChanges.value.get(uiId);
        if (!change) return null;

        return getPayloadPosition(change.payload);
    };

    const { createExistingEvent, createSuggestionEvent } = useEventWrapper(resolveStagedPosition);

    const existingEvents = computed(() => timeEntryStore.timeEntries.map((c) => createExistingEvent(c)));
    const suggestionEvents = computed(() => suggestionStore.timeEntrySuggestions.map((c) => createSuggestionEvent(c)));

    // A draft lives in its staged create, removed or not: a removed one keeps rendering
    // so it can be brought back. Sorted, because map order shifts on revert.
    const draftEvents = computed<readonly DraftTimeEntryEvent[]>(() => {
        const changes = [...stagedChanges.value.values()];
        const drafts = changes.map(getDraftEvent).filter((draft) => draft !== null);

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

    // A reload replaces every contract and with it every uiId, so the batch it
    // referred to is gone. Dropping it beats keeping changes nothing can show.
    watch(
        filter,
        () => {
            if (activeCommits.value > 0) return;

            stagedChanges.value.clear();
            gesture.value = { kind: "idle" };
            task.value = { kind: "none" };
        },
        { deep: true }
    );

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
