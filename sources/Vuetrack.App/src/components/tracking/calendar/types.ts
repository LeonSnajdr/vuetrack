import type { TimeEntryCreateContract, TimeEntryContract, TimeEntryUpdateContract, TimeEntryId } from "@/contracts/TimeEntryContract";
import type { TimeEntrySuggestionContract, TimeEntrySuggestionUpdateContract, TimeEntrySuggestionId } from "@/contracts/TimeEntrySuggestion";
import type { Nullable } from "@/util/Nullable";
import type { ValidationErrors } from "@/util/ValidationProblem";
import type { CalendarEvent } from "vuetify/lib/components/VCalendar/types.mjs";

export type EventEdge = "start" | "end";

export type EventPosition = {
    start: number;
    end: number;
};

export type BaseCalendarEvent = {
    uiId: string;
    timed: boolean;
} & EventPosition;

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

export type SuggestionTimeEntryCreateMutation = {
    kind: "create";
    event: SuggestionTimeEntryEvent;
    create: TimeEntryCreatePayload;
};

export type SuggestionTimeEntryDeleteMutation = {
    kind: "delete";
    event: SuggestionTimeEntryEvent;
    id: TimeEntrySuggestionId;
};

export type DraftTimeEntryDeleteMutation = {
    kind: "delete";
    event: DraftTimeEntryEvent;
};

export type DraftTimeEntryCreateMutation = {
    kind: "create";
    event: DraftTimeEntryEvent;
    create: TimeEntryCreatePayload;
};

export type TimeEntryUpdateMutation = ExistingTimeEntryUpdateMutation | SuggestionTimeEntryUpdateMutation;
export type TimeEntryCreateMutation = DraftTimeEntryCreateMutation | SuggestionTimeEntryCreateMutation;
export type TimeEntryDeleteMutation = DraftTimeEntryDeleteMutation | ExistingTimeEntryDeleteMutation | SuggestionTimeEntryDeleteMutation;
export type TimeEntryMutation = TimeEntryUpdateMutation | TimeEntryCreateMutation | TimeEntryDeleteMutation;

// Transient pointer state. Never touches the API.
export type Gesture =
    | { kind: "idle" }
    | {
          kind: "move";
          event: TimeEntryEvent;
          from: EventPosition;
          pointerOffsetMs?: number;
      }
    | {
          kind: "resize";
          edge: EventEdge;
          event: TimeEntryEvent;
          from: EventPosition;
      }
    | {
          kind: "draft";
          event: DraftTimeEntryEvent;
          anchorStartMs: number;
      };

// Modal intent. Drives the overlays and gates gestures.
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
          event: TimeEntryEvent;
      };

export type ConflictTask = Extract<Task, { kind: "conflict" }>;

// Stores the position the event started from; the event itself carries the target.
export type StagedChange =
    | {
          kind: "update";
          event: PositionableEvent;
          from: EventPosition;
          payload?: TimeEntryUpdatePayload;
      }
    | {
          kind: "remove";
          event: TimeEntryEvent;
      }
    | {
          kind: "add";
          event: CreatableEvent;
          payload: TimeEntryCreatePayload;
      };

export type GestureKind = Gesture["kind"];
export type TaskKind = Task["kind"];
export type StagedChangeKind = StagedChange["kind"];

export function isTimeEntryEvent(e: CalendarEvent): e is TimeEntryEvent {
    return e.kind === "suggestion" || e.kind === "existing" || e.kind === "draft";
}

export function isExistingUpdateMutation(mutation: TimeEntryUpdateMutation): mutation is ExistingTimeEntryUpdateMutation {
    return mutation.event.kind === "existing";
}
