import type { TimeEntryEvent, UiId } from "@/components/tracking/calendar/types";
import type { Occupied } from "@/components/tracking/calendar/conflictResolvers";
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

    // What the resolutions reason over: ranges, not events to be poked at.
    const occupied = computed<Occupied[]>(() => {
        return candidates.value.map((event) => ({ event, position: { start: event.start, end: event.end } }));
    });

    // Each unordered pair only once: everything after the subject, never before it.
    const conflictPairs = computed<ConflictPair[]>(() => {
        const pairs: ConflictPair[] = [];

        for (const [index, first] of candidates.value.entries()) {
            const others = candidates.value.slice(index + 1);

            for (const second of others) {
                if (!isOverlapping(first, second)) continue;

                pairs.push({ first, second });
            }
        }

        return pairs;
    });

    const conflictingUiIds = computed<Set<UiId>>(() => {
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

    return { candidates, occupied, conflictPairs, conflictingUiIds, hasConflicts, getOverlapsFor, tryEnterConflict };
}
