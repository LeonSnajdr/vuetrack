import type { TimeEntryCreateContract, TimeEntryContract, TimeEntryUpdateContract, TimeEntryId } from "@/contracts/TimeEntryContract";
import type { TimeEntrySuggestionContract, TimeEntrySuggestionUpdateContract, TimeEntrySuggestionId } from "@/contracts/TimeEntrySuggestion";
import type { Nullable } from "@/util/Nullable";
import type { ValidationErrors } from "@/util/ValidationProblem";
import type { CalendarEvent } from "vuetify/lib/components/VCalendar/types.mjs";
import type { Branded } from "typings/brand";

export type UiId = Branded<string, "uiId">;

export type EventEdge = "start" | "end";

export type EventPosition = {
    start: number;
    end: number;
};

export type BaseCalendarEvent = {
    uiId: UiId;
    timed: boolean;
    readonly start: number;
    readonly end: number;
};

export type DraftTimeEntryEvent = {
    kind: "draft";
    createEntry: Nullable<TimeEntryCreateContract>;
} & BaseCalendarEvent;

export type ExistingTimeEntryEvent = {
    kind: "existing";
    timeEntry: TimeEntryContract;
} & BaseCalendarEvent;

export type SuggestionTimeEntryEvent = {
    kind: "suggestion";
    timeEntry: TimeEntrySuggestionContract;
} & BaseCalendarEvent;

export type TimeEntryEvent = DraftTimeEntryEvent | ExistingTimeEntryEvent | SuggestionTimeEntryEvent;
export type PositionableEvent = ExistingTimeEntryEvent | SuggestionTimeEntryEvent;
export type CreatableEvent = DraftTimeEntryEvent | SuggestionTimeEntryEvent;

export type TimeEntryUpdatePayload = TimeEntryUpdateContract | TimeEntrySuggestionUpdateContract;
export type TimeEntryCreatePayload = Nullable<TimeEntryCreateContract>;

export type ExistingTimeEntryUpdateMutation = {
    kind: "update";
    event: ExistingTimeEntryEvent;
    update: TimeEntryUpdateContract;
};

export type ExistingTimeEntryDeleteMutation = {
    kind: "delete";
    event: ExistingTimeEntryEvent;
    id: TimeEntryId;
};

export type SuggestionTimeEntryUpdateMutation = {
    kind: "update";
    event: SuggestionTimeEntryEvent;
    update: TimeEntrySuggestionUpdateContract;
};

export type SuggestionTimeEntryDeleteMutation = {
    kind: "delete";
    event: SuggestionTimeEntryEvent;
    id: TimeEntrySuggestionId;
};

export type TimeEntryCreateMutation = {
    kind: "create";
    event: CreatableEvent;
    create: TimeEntryCreatePayload;
};

export type TimeEntryUpdateMutation = ExistingTimeEntryUpdateMutation | SuggestionTimeEntryUpdateMutation;
export type TimeEntryDeleteMutation = ExistingTimeEntryDeleteMutation | SuggestionTimeEntryDeleteMutation;
export type TimeEntryMutation = TimeEntryUpdateMutation | TimeEntryCreateMutation | TimeEntryDeleteMutation;

export type Gesture =
    | { kind: "idle" }
    | {
          kind: "move";
          event: TimeEntryEvent;
          from: EventPosition;
          wasStaged: boolean;
          pointerOffsetMs?: number;
      }
    | {
          kind: "resize";
          edge: EventEdge;
          event: TimeEntryEvent;
          from: EventPosition;
          wasStaged: boolean;
      }
    | {
          kind: "draft";
          event: DraftTimeEntryEvent;
          anchorStartMs: number;
      };

export type Task =
    | { kind: "none" }
    | {
          kind: "create";
          event: CreatableEvent;
          payload: TimeEntryCreatePayload;
          errors?: ValidationErrors;
      }
    | {
          kind: "edit";
          event: PositionableEvent;
          payload: TimeEntryUpdatePayload;
          errors?: ValidationErrors;
      }
    | {
          kind: "conflict";
          event: TimeEntryEvent;
      }
    | {
          kind: "delete";
          event: PositionableEvent;
      };

export type ConflictTask = Extract<Task, { kind: "conflict" }>;

export type StagedSaveChange = {
    kind: "save";
    event: PositionableEvent;
    payload: TimeEntryUpdatePayload;
    removed: boolean;
};

export type StagedCreateChange = {
    kind: "create";
    event: CreatableEvent;
    payload: TimeEntryCreatePayload;
    removed: boolean;
};

export type StagedChange = StagedSaveChange | StagedCreateChange;
export type StagedPayload = TimeEntryUpdatePayload | TimeEntryCreatePayload;

export type GestureKind = Gesture["kind"];
export type TaskKind = Task["kind"];
export type StagedChangeKind = StagedChange["kind"];

export function isTimeEntryEvent(e: CalendarEvent): e is TimeEntryEvent {
    return e.kind === "suggestion" || e.kind === "existing" || e.kind === "draft";
}

export function getDraftEvent(change: StagedChange): DraftTimeEntryEvent | null {
    if (change.kind !== "create") return null;
    if (change.event.kind !== "draft") return null;

    return change.event;
}

export function getPayloadPosition(payload: StagedPayload): EventPosition | null {
    if (!payload.dateStarted || !payload.dateEnded) return null;

    return { start: payload.dateStarted.getTime(), end: payload.dateEnded.getTime() };
}

export function isExistingUpdateMutation(mutation: TimeEntryUpdateMutation): mutation is ExistingTimeEntryUpdateMutation {
    return mutation.event.kind === "existing";
}
