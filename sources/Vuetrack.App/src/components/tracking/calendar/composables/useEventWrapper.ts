import type { TimeEntryContract, TimeEntryCreateContract } from "@/contracts/TimeEntryContract";
import type { TimeEntrySuggestionContract } from "@/contracts/TimeEntrySuggestion";
import type { Nullable } from "@/util/Nullable";
import type { DraftTimeEntryEvent, ExistingTimeEntryEvent, SuggestionTimeEntryEvent } from "@/components/tracking/calendar/types";
import { useCalendarHelper } from "./useCalendarHelper";

const existingWrapperCache = new WeakMap<TimeEntryContract, ExistingTimeEntryEvent>();
const suggestionWrapperCache = new WeakMap<TimeEntrySuggestionContract, SuggestionTimeEntryEvent>();

export function useEventWrapper() {
    const { minimumEventDurationMs } = useCalendarHelper();
    const timeEntryHelper = useTimeEntryHelper();

    const createExistingEvent = (contract: TimeEntryContract): ExistingTimeEntryEvent => {
        const cached = existingWrapperCache.get(contract);
        if (cached) return cached;

        const wrapper: ExistingTimeEntryEvent = {
            kind: "existing",
            timed: true,
            uiId: `event-uiId-${crypto.randomUUID()}`,
            timeEntry: contract,
            get start() {
                return this.timeEntry.dateStarted.getTime();
            },
            set start(ms: number) {
                this.timeEntry.dateStarted = new Date(ms);
            },
            get end() {
                return this.timeEntry.dateEnded.getTime();
            },
            set end(ms: number) {
                this.timeEntry.dateEnded = new Date(ms);
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
            uiId: `event-uiId-${crypto.randomUUID()}`,
            timeEntry: contract,
            get start() {
                return this.timeEntry.dateStarted.getTime();
            },
            set start(ms: number) {
                this.timeEntry.dateStarted = new Date(ms);
            },
            get end() {
                return this.timeEntry.dateEnded.getTime();
            },
            set end(ms: number) {
                this.timeEntry.dateEnded = new Date(ms);
            }
        };
        suggestionWrapperCache.set(contract, wrapper);
        return wrapper;
    };

    const buildDraftEvent = (createEntry: Nullable<TimeEntryCreateContract>, fallbackStartMs: number, fallbackEndMs: number): DraftTimeEntryEvent => {
        return {
            kind: "draft",
            timed: true,
            uiId: `event-uiId-${crypto.randomUUID()}`,
            createEntry,
            get start() {
                const dateStarted = this.createEntry.dateStarted;
                if (!dateStarted) return fallbackStartMs;
                return dateStarted.getTime();
            },
            set start(ms: number) {
                this.createEntry.dateStarted = new Date(ms);
            },
            get end() {
                const dateEnded = this.createEntry.dateEnded;
                if (!dateEnded) return fallbackEndMs;
                return dateEnded.getTime();
            },
            set end(ms: number) {
                this.createEntry.dateEnded = new Date(ms);
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

    const cloneEventAsDraft = (source: ExistingTimeEntryEvent | SuggestionTimeEntryEvent, start: number, end: number): DraftTimeEntryEvent => {
        const sourceEntry = source.timeEntry;
        const createEntry = {
            taskId: sourceEntry.taskId,
            projectId: source.kind === "existing" ? source.timeEntry.project.id : source.timeEntry.projectId,
            activityId: source.kind === "existing" ? source.timeEntry.activity.id : source.timeEntry.activityId,
            comment: sourceEntry.comment,
            dateStarted: new Date(start),
            dateEnded: new Date(end)
        };

        return buildDraftEvent(createEntry, start, end);
    };

    return {
        createExistingEvent,
        createSuggestionEvent,
        createDraftEvent,
        cloneEventAsDraft
    };
}
