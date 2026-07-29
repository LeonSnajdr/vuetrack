import type { TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useCalendarHelper } from "./useCalendarHelper";
import { useChangeSet } from "./useChangeSet";
import { useConflict } from "./useConflict";
import { useConflictDetection } from "./useConflictDetection";
import { useEventWrapper } from "./useEventWrapper";

export interface ConflictResolutionStrategy {
    id: string;
    label: string;
    subtitle: string;
    icon: string;
    // Set on resolutions that reshape other entries.
    color?: "error";
    resolve: () => boolean;
}

// Automatic resolutions for the selected event. Each one only stages its changes.
export function useConflictStrategies() {
    const changeSet = useChangeSet();
    const conflict = useConflict();
    const detection = useConflictDetection();

    const { t } = useI18n();
    const { startOfDay, addDays } = useDateHelper();
    const { applyEventPosition } = useCalendarHelper();
    const { cloneAsDraft } = useEventWrapper();

    const strategies = computed<ConflictResolutionStrategy[]>(() => [
        {
            id: "shift-up",
            label: t("calendar.conflict.strategy.movePrevious"),
            subtitle: t("calendar.conflict.strategy.movePrevious.subtitle"),
            icon: mdiArrowUpThin,
            resolve: resolveShiftUp
        },
        {
            id: "shift-down",
            label: t("calendar.conflict.strategy.moveNext"),
            subtitle: t("calendar.conflict.strategy.moveNext.subtitle"),
            icon: mdiArrowDownThin,
            resolve: resolveShiftDown
        },
        {
            id: "truncate",
            label: t("calendar.conflict.strategy.fitToGap"),
            subtitle: t("calendar.conflict.strategy.fitToGap.subtitle"),
            icon: mdiArrowCollapseVertical,
            resolve: resolveTruncate
        },
        {
            id: "force",
            label: t("calendar.conflict.strategy.forcePosition"),
            subtitle: t("calendar.conflict.strategy.forcePosition.subtitle"),
            icon: mdiAlertBoxOutline,
            color: "error",
            resolve: resolveForce
        }
    ]);

    const getSearchWindow = (event: TimeEntryEvent) => {
        const windowStart = startOfDay(new Date(event.start)).getTime();
        const lastOccupiedMs = Math.max(event.start, event.end - 1);
        const lastOccupiedDay = startOfDay(new Date(lastOccupiedMs));
        const windowEndExclusive = addDays(lastOccupiedDay, 1).getTime();

        return { windowStart, windowEndExclusive };
    };

    // Includes the unsaved conflict event when the selection is another one.
    const getSearchCandidates = (event: TimeEntryEvent) => {
        const { windowStart, windowEndExclusive } = getSearchWindow(event);
        const candidates = [...detection.candidates.value]
            .filter((candidate) => candidate.uiId !== event.uiId)
            .filter((candidate) => !changeSet.isRemoved(candidate.uiId))
            .sort((a, b) => a.start - b.start)
            .filter((candidate) => candidate.end > windowStart && candidate.start < windowEndExclusive);

        return { candidates, windowStart, windowEndExclusive };
    };

    const moveTo = (event: TimeEntryEvent, newStart: number, newEnd: number): void => {
        changeSet.stagePosition(event);
        applyEventPosition(event, newStart, newEnd);
    };

    const resolveShiftUp = (): boolean => {
        const event = conflict.selectedEvent.value;
        if (!event) return false;

        const duration = event.end - event.start;
        const { candidates, windowStart, windowEndExclusive } = getSearchCandidates(event);

        let searchTime = event.start;

        while (searchTime >= windowStart) {
            const potentialStart = searchTime;
            const potentialEnd = searchTime + duration;

            if (potentialEnd > windowEndExclusive) {
                searchTime = windowEndExclusive - duration;
                continue;
            }

            const overlap = candidates.find((candidate) => potentialStart < candidate.end && potentialEnd > candidate.start);
            if (overlap) {
                searchTime = overlap.start - duration;
            } else {
                moveTo(event, potentialStart, potentialEnd);
                return true;
            }
        }

        return false;
    };

    const resolveShiftDown = (): boolean => {
        const event = conflict.selectedEvent.value;
        if (!event) return false;

        const duration = event.end - event.start;
        const { candidates, windowEndExclusive } = getSearchCandidates(event);

        let searchTime = event.start;

        while (searchTime + duration <= windowEndExclusive) {
            const overlap = candidates.find((candidate) => searchTime < candidate.end && searchTime + duration > candidate.start);
            if (overlap) {
                searchTime = overlap.end;
            } else {
                const foundEnd = searchTime + duration;
                moveTo(event, searchTime, foundEnd);
                return true;
            }
        }

        return false;
    };

    const resolveTruncate = (): boolean => {
        const event = conflict.selectedEvent.value;
        if (!event) return false;

        const overlaps = detection.getOverlapsFor(event);

        let allowedStart = event.start;
        let allowedEnd = event.end;

        const isFullyContained = overlaps.some((overlap) => overlap.start <= event.start && overlap.end >= event.end);
        if (isFullyContained) return false;

        overlaps.forEach((overlap) => {
            if (overlap.start > event.start && overlap.start < event.end) {
                allowedEnd = Math.min(allowedEnd, overlap.start);
            }
            if (overlap.end > event.start && overlap.end < event.end) {
                allowedStart = Math.max(allowedStart, overlap.end);
            }
        });

        if (allowedEnd <= allowedStart) return false;

        moveTo(event, allowedStart, allowedEnd);
        return true;
    };

    const resolveForce = (): boolean => {
        const event = conflict.selectedEvent.value;
        if (!event) return false;

        const overlaps = detection.getOverlapsFor(event);

        const splitOverlap = (overlap: TimeEntryEvent, headEnd: number, tailStart: number): void => {
            const tailEnd = overlap.end;
            moveTo(overlap, overlap.start, headEnd);

            const tailEvent = cloneAsDraft(overlap, tailStart, tailEnd);
            changeSet.stageDraft(tailEvent);
        };

        for (const overlap of overlaps) {
            // Completely overlapped - remove it
            if (event.start <= overlap.start && event.end >= overlap.end) {
                changeSet.stageRemove(overlap);
                continue;
            }

            // Event sits inside the overlap - split it: head shrinks, tail becomes a new entry
            if (event.start > overlap.start && event.end < overlap.end) {
                splitOverlap(overlap, event.start, event.end);
                continue;
            }

            // Partially overlapped - truncate it
            if (event.start > overlap.start && event.start < overlap.end) {
                moveTo(overlap, overlap.start, event.start);
            }

            if (event.end > overlap.start && event.end < overlap.end) {
                moveTo(overlap, event.end, overlap.end);
            }
        }

        return true;
    };

    return { strategies };
}
