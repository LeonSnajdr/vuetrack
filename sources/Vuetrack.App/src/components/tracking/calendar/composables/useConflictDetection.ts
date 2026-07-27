import type { TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useCalendarHelper } from "./useCalendarHelper";
import { useChangeSet } from "./useChangeSet";
import { useEventSelection } from "./useEventSelection";

export type ConflictPair = {
    first: TimeEntryEvent;
    second: TimeEntryEvent;
};

export function useConflictDetection() {
    const calendarStore = useCalendarStore();
    const changeSet = useChangeSet();
    const { select } = useEventSelection();
    const { isOverlapping, getOverlappingEvents } = useCalendarHelper();

    const { existingEvents, task } = storeToRefs(calendarStore);

    // Suggestions are not obstacles: only stored entries plus the event the
    // current conflict is about can collide with each other.
    const candidates = computed<TimeEntryEvent[]>(() => {
        const conflictEvent = task.value.kind === "conflict" ? task.value.event : null;
        const stored = existingEvents.value.filter((event) => !changeSet.isRemoved(event.uiId));

        if (!conflictEvent || conflictEvent.kind === "existing") return stored;
        if (changeSet.isRemoved(conflictEvent.uiId)) return stored;

        return [...stored, conflictEvent];
    });

    const conflictPairs = computed<ConflictPair[]>(() => {
        const pairs = new Map<string, ConflictPair>();

        for (const first of candidates.value) {
            for (const second of candidates.value) {
                if (!isOverlapping(first, second)) continue;

                const key = [first.uiId, second.uiId].sort().join("|");
                if (pairs.has(key)) continue;

                pairs.set(key, { first, second });
            }
        }

        return [...pairs.values()];
    });

    const conflictingUiIds = computed<Set<string>>(() => {
        const uiIds = conflictPairs.value.flatMap((pair) => [pair.first.uiId, pair.second.uiId]);
        return new Set(uiIds);
    });

    const hasConflicts = computed(() => conflictPairs.value.length > 0);

    // Live overlaps of a single event. The conflict task holds no snapshot: every
    // preview and every drag changes who overlaps whom.
    const getOverlapsFor = (event: TimeEntryEvent): TimeEntryEvent[] => {
        const obstacles = candidates.value.filter((candidate) => candidate.uiId !== event.uiId);
        return getOverlappingEvents(event, obstacles);
    };

    // Opens the conflict task when the event collides with stored entries. The
    // staged changes are deliberately left in place: they are the unsaved state
    // the conflict panel lets the user work on.
    const tryEnterConflict = (event: TimeEntryEvent): boolean => {
        const overlaps = getOverlappingEvents(event, existingEvents.value);
        if (overlaps.length === 0) return false;

        // The offending event is what the resolutions should act on first.
        select(event);

        task.value = { kind: "conflict", event };
        return true;
    };

    return { candidates, conflictPairs, conflictingUiIds, hasConflicts, getOverlapsFor, tryEnterConflict };
}
