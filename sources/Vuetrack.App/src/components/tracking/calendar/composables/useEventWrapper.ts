import type { TimeEntryContract, TimeEntryCreateContract } from "@/contracts/TimeEntryContract";
import type { TimeEntrySuggestionContract } from "@/contracts/TimeEntrySuggestion";
import type { Nullable } from "@/util/Nullable";
import type { DraftTimeEntryEvent, EventPosition, ExistingTimeEntryEvent, SuggestionTimeEntryEvent, TimeEntryEvent, UiId } from "@/components/tracking/calendar/types";
import { useCalendarHelper } from "./useCalendarHelper";

const existingWrapperCache = new WeakMap<TimeEntryContract, ExistingTimeEntryEvent>();
const suggestionWrapperCache = new WeakMap<TimeEntrySuggestionContract, SuggestionTimeEntryEvent>();

// Read-only positions: what a gesture proposes lives in the change set, never in the contract.
export type StagedPositionResolver = (uiId: UiId) => EventPosition | null;

// Prefixed because a uiId doubles as a DOM element id, and a raw uuid may start with a digit.
function createUiId(): UiId {
    const uuid = crypto.randomUUID();
    return `event-uiId-${uuid}` as UiId;
}

export function useEventWrapper(resolveStaged: StagedPositionResolver = () => null) {
    const { minimumEventDurationMs } = useCalendarHelper();
    const timeEntryHelper = useTimeEntryHelper();

    const createExistingEvent = (contract: TimeEntryContract): ExistingTimeEntryEvent => {
        const cached = existingWrapperCache.get(contract);
        if (cached) return cached;

        const wrapper: ExistingTimeEntryEvent = {
            kind: "existing",
            timed: true,
            uiId: createUiId(),
            timeEntry: contract,
            get start() {
                const staged = resolveStaged(this.uiId);
                if (staged) return staged.start;

                return this.timeEntry.dateStarted.getTime();
            },
            get end() {
                const staged = resolveStaged(this.uiId);
                if (staged) return staged.end;

                return this.timeEntry.dateEnded.getTime();
            }
        };
        existingWrapperCache.set(contract, wrapper);
        return wrapper;
    };

    const createSuggestionEvent = (contract: TimeEntrySuggestionContract): SuggestionTimeEntryEvent => {
        const cached = suggestionWrapperCache.get(contract);
        if (cached) return cached;

        const wrapper: SuggestionTimeEntryEvent = {
            kind: "suggestion",
            timed: true,
            uiId: createUiId(),
            timeEntry: contract,
            get start() {
                const staged = resolveStaged(this.uiId);
                if (staged) return staged.start;

                return this.timeEntry.dateStarted.getTime();
            },
            get end() {
                const staged = resolveStaged(this.uiId);
                if (staged) return staged.end;

                return this.timeEntry.dateEnded.getTime();
            }
        };
        suggestionWrapperCache.set(contract, wrapper);
        return wrapper;
    };

    const buildDraftEvent = (createEntry: Nullable<TimeEntryCreateContract>, fallbackStartMs: number, fallbackEndMs: number): DraftTimeEntryEvent => {
        return {
            kind: "draft",
            timed: true,
            uiId: createUiId(),
            createEntry,
            get start() {
                const dateStarted = this.createEntry.dateStarted;
                if (!dateStarted) return fallbackStartMs;

                return dateStarted.getTime();
            },
            get end() {
                const dateEnded = this.createEntry.dateEnded;
                if (!dateEnded) return fallbackEndMs;

                return dateEnded.getTime();
            }
        };
    };

    const createDraftEvent = (anchorStartMs: number): DraftTimeEntryEvent => {
        const anchorEndMs = anchorStartMs + minimumEventDurationMs;
        const createEntry = timeEntryHelper.createDefaultTimeEntry({
            dateStarted: new Date(anchorStartMs),
            dateEnded: new Date(anchorEndMs)
        });

        return buildDraftEvent(createEntry, anchorStartMs, anchorEndMs);
    };

    const buildCloneEntry = (source: TimeEntryEvent): Nullable<TimeEntryCreateContract> => {
        if (source.kind === "draft") return { ...source.createEntry };
        if (source.kind === "existing") {
            return {
                taskId: source.timeEntry.taskId,
                projectId: source.timeEntry.project.id,
                activityId: source.timeEntry.activity.id,
                comment: source.timeEntry.comment,
                dateStarted: null,
                dateEnded: null
            };
        }

        return {
            taskId: source.timeEntry.taskId,
            projectId: source.timeEntry.projectId,
            activityId: source.timeEntry.activityId,
            comment: source.timeEntry.comment,
            dateStarted: null,
            dateEnded: null
        };
    };

    // Anything can be cloned into a draft: what differs is only where the fields sit.
    const cloneAsDraft = (source: TimeEntryEvent, start: number, end: number): DraftTimeEntryEvent => {
        const createEntry = buildCloneEntry(source);
        createEntry.dateStarted = new Date(start);
        createEntry.dateEnded = new Date(end);

        return buildDraftEvent(createEntry, start, end);
    };

    return {
        createExistingEvent,
        createSuggestionEvent,
        createDraftEvent,
        cloneAsDraft
    };
}
