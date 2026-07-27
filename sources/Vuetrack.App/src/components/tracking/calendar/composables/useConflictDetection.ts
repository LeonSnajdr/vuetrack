import type { TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useCalendarHelper } from "./useCalendarHelper";
import { useChangeSet } from "./useChangeSet";

export type ConflictPair = {
    key: string;
    first: TimeEntryEvent;
    second: TimeEntryEvent;
};

export function useConflictDetection() {
    const calendarStore = useCalendarStore();
    const changeSet = useChangeSet();
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

                pairs.set(key, { key, first, second });
            }
        }

        return [...pairs.values()];
    });

    const conflictingUiIds = computed<Set<string>>(() => {
        const uiIds = conflictPairs.value.flatMap((pair) => [pair.first.uiId, pair.second.uiId]);
        return new Set(uiIds);
    });

    const hasConflicts = computed(() => conflictPairs.value.length > 0);

    // Opens the conflict task when the event collides with stored entries. The
    // staged changes are deliberately left in place: they are the unsaved state
    // the conflict panel lets the user work on.
    const tryEnterConflict = (event: TimeEntryEvent): boolean => {
        const overlaps = getOverlappingEvents(event, existingEvents.value);
        if (overlaps.length === 0) return false;

        task.value = { kind: "conflict", event, overlaps, mode: "strategies" };
        return true;
    };

    return { candidates, conflictPairs, conflictingUiIds, hasConflicts, tryEnterConflict };
}
