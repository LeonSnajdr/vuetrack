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

    const { events, task } = storeToRefs(calendarStore);

    // Suggestions are not obstacles. Everything else claims its time, drafts included.
    const candidates = computed<TimeEntryEvent[]>(() => {
        const conflictEvent = task.value.kind === "conflict" ? task.value.event : null;
        const stored = events.value.filter((event) => event.kind !== "suggestion").filter((event) => !changeSet.isRemoved(event.uiId));

        if (!conflictEvent || conflictEvent.kind !== "suggestion" || changeSet.isRemoved(conflictEvent.uiId)) return stored;

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

    // Never snapshotted: every preview and drag changes who overlaps whom.
    const getOverlapsFor = (event: TimeEntryEvent): TimeEntryEvent[] => {
        const obstacles = candidates.value.filter((candidate) => candidate.uiId !== event.uiId);
        return getOverlappingEvents(event, obstacles);
    };

    // Staged changes stay in place: they are what the conflict panel works on.
    const tryEnterConflict = (event: TimeEntryEvent): boolean => {
        const overlaps = getOverlappingEvents(event, candidates.value);
        if (overlaps.length === 0) return false;

        select(event);

        task.value = { kind: "conflict", event };
        return true;
    };

    return { candidates, conflictPairs, conflictingUiIds, hasConflicts, getOverlapsFor, tryEnterConflict };
}
