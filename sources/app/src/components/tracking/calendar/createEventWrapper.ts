import type { TimeEntryContract, TimeEntryCreateContract } from "@/contracts/TimeEntryContract";
import type { TimeEntrySuggestionContract } from "@/contracts/TimeEntrySuggestion";
import type { DraftTimeEntryEvent, ExistingTimeEntryEvent, SuggestionTimeEntryEvent } from "./types";
import type { ActivityId } from "@/contracts/ActivityContract";
import type { ProjectId } from "@/contracts/ProjectContract";

const existingWrapperCache = new WeakMap<TimeEntryContract, ExistingTimeEntryEvent>();
const suggestionWrapperCache = new WeakMap<TimeEntrySuggestionContract, SuggestionTimeEntryEvent>();

export function createExistingEventWrapper(contract: TimeEntryContract, color = "#7da6c9"): ExistingTimeEntryEvent {
    const cached = existingWrapperCache.get(contract);
    if (cached) return cached;

    const wrapper: ExistingTimeEntryEvent = {
        kind: "existing",
        color,
        timed: true,
        uiId: `event-uiId-${uuidv4()}`,
        timeEntry: contract,
        get start() {
            return contract.startTime.getTime();
        },
        set start(ms: number) {
            contract.startTime = new Date(ms);
        },
        get end() {
            return contract.endTime.getTime();
        },
        set end(ms: number) {
            contract.endTime = new Date(ms);
        }
    };
    existingWrapperCache.set(contract, wrapper);
    return wrapper;
}

export function createSuggestionEventWrapper(contract: TimeEntrySuggestionContract, color = "#22C55E"): SuggestionTimeEntryEvent {
    const cached = suggestionWrapperCache.get(contract);
    if (cached) return cached;

    const wrapper: SuggestionTimeEntryEvent = {
        kind: "suggestion",
        color,
        timed: true,
        uiId: `event-uiId-${uuidv4()}`,
        timeEntry: contract,
        get start() {
            return contract.startTime.getTime();
        },
        set start(ms: number) {
            contract.startTime = new Date(ms);
        },
        get end() {
            return contract.endTime.getTime();
        },
        set end(ms: number) {
            contract.endTime = new Date(ms);
        }
    };
    suggestionWrapperCache.set(contract, wrapper);
    return wrapper;
}

export function createDraftEvent(anchorStartMs: number, taskId = ""): DraftTimeEntryEvent {
    const createEntry: TimeEntryCreateContract = {
        startTime: new Date(anchorStartMs),
        endTime: new Date(anchorStartMs),
        taskId,
        activityId: undefined as unknown as ActivityId,
        projectId: undefined as unknown as ProjectId,
        comment: ""
    };

    return {
        kind: "draft",
        color: "#673AB7",
        timed: true,
        uiId: `event-uiId-${uuidv4()}`,
        createEntry,
        get start() {
            return createEntry.startTime.getTime();
        },
        set start(ms: number) {
            createEntry.startTime = new Date(ms);
        },
        get end() {
            return createEntry.endTime.getTime();
        },
        set end(ms: number) {
            createEntry.endTime = new Date(ms);
        }
    };
}
