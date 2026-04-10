import type { TimeEntryContract } from "@/contracts/TimeEntryContract";
import type { TimeEntrySuggestionContract } from "@/contracts/TimeEntrySuggestion";
import type { DraftTimeEntryEvent, ExistingTimeEntryEvent, SuggestionTimeEntryEvent } from "@/components/tracking/calendar/types";
import { useCalendarHelper } from "./useCalendarHelper";

const existingWrapperCache = new WeakMap<TimeEntryContract, ExistingTimeEntryEvent>();
const suggestionWrapperCache = new WeakMap<TimeEntrySuggestionContract, SuggestionTimeEntryEvent>();

export function useEventWrapper() {
    const { minimumEventDurationMs } = useCalendarHelper();
    const timeEntryHelper = useTimeEntryHelper();

    const createExistingEvent = (contract: TimeEntryContract, color = "#7da6c9"): ExistingTimeEntryEvent => {
        const cached = existingWrapperCache.get(contract);
        if (cached) return cached;

        const wrapper: ExistingTimeEntryEvent = {
            kind: "existing",
            color,
            timed: true,
            uiId: `event-uiId-${uuidv4()}`,
            timeEntry: contract,
            get start() {
                return this.timeEntry.startTime.getTime();
            },
            set start(ms: number) {
                this.timeEntry.startTime = new Date(ms);
            },
            get end() {
                return this.timeEntry.endTime.getTime();
            },
            set end(ms: number) {
                this.timeEntry.endTime = new Date(ms);
            }
        };
        existingWrapperCache.set(contract, wrapper);
        return wrapper;
    };

    const createSuggestionEvent = (contract: TimeEntrySuggestionContract, color = "#22C55E"): SuggestionTimeEntryEvent => {
        const cached = suggestionWrapperCache.get(contract);
        if (cached) return cached;

        const wrapper: SuggestionTimeEntryEvent = {
            kind: "suggestion",
            color,
            timed: true,
            uiId: `event-uiId-${uuidv4()}`,
            timeEntry: contract,
            get start() {
                return this.timeEntry.startTime.getTime();
            },
            set start(ms: number) {
                this.timeEntry.startTime = new Date(ms);
            },
            get end() {
                return this.timeEntry.endTime.getTime();
            },
            set end(ms: number) {
                this.timeEntry.endTime = new Date(ms);
            }
        };
        suggestionWrapperCache.set(contract, wrapper);
        return wrapper;
    };

    const createDraftEvent = (anchorStartMs: number): DraftTimeEntryEvent => {
        const createEntry = timeEntryHelper.createDefaultTimeEntry({
            startTime: new Date(anchorStartMs),
            endTime: new Date(anchorStartMs + minimumEventDurationMs)
        });

        return {
            kind: "draft",
            color: "#673AB7",
            timed: true,
            uiId: `event-uiId-${uuidv4()}`,
            createEntry,
            get start() {
                return this.createEntry.startTime.getTime();
            },
            set start(ms: number) {
                this.createEntry.startTime = new Date(ms);
            },
            get end() {
                return this.createEntry.endTime.getTime();
            },
            set end(ms: number) {
                this.createEntry.endTime = new Date(ms);
            }
        };
    };

    return {
        createExistingEvent,
        createSuggestionEvent,
        createDraftEvent
    };
}
