import type { TimeEntryCreateContract, TimeEntryContract, TimeEntryUpdateContract, TimeEntryId } from "@/contracts/TimeEntryContract";
import type { TimeEntrySuggestionContract, TimeEntrySuggestionUpdateContract, TimeEntrySuggestionId } from "@/contracts/TimeEntrySuggestion";
import type { CalendarEvent } from "vuetify/lib/components/VCalendar/types.mjs";

export type EventPosition = {
    start: number;
    end: number;
};

export type BaseCalendarEvent = {
    uiId: string;
    timed: boolean;
    color: string;
} & EventPosition;

export type DraftTimeEntryEvent = {
    kind: "draft";
    createEntry: TimeEntryCreateContract;
} & BaseCalendarEvent;

export type ExistingTimeEntryEvent = {
    kind: "existing";
    timeEntry: TimeEntryContract;
} & BaseCalendarEvent;

export type ExistingTimeEntryUpdateMutation = {
    kind: "update";
    event: ExistingTimeEntryEvent;
    update: TimeEntryUpdateContract;
    originalPosition: EventPosition;
};

export type ExistingTimeEntryDeleteMutation = {
    kind: "delete";
    event: ExistingTimeEntryEvent;
    id: TimeEntryId;
};

export type SuggestionTimeEntryEvent = {
    kind: "suggestion";
    timeEntry: TimeEntrySuggestionContract;
} & BaseCalendarEvent;

export type SuggestionTimeEntryUpdateMutation = {
    kind: "update";
    event: SuggestionTimeEntryEvent;
    update: TimeEntrySuggestionUpdateContract;
    originalPosition: EventPosition;
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
    create: TimeEntryCreateContract;
    originalPosition: EventPosition;
};

export type SuggestionTimeEntryCreateMutation = {
    kind: "create";
    event: SuggestionTimeEntryEvent;
    create: {
        taskId: string;
        startTime: Date;
        endTime: Date;
    };
    originalPosition: EventPosition;
};

export type TimeEntryEvent = DraftTimeEntryEvent | ExistingTimeEntryEvent | SuggestionTimeEntryEvent;
export type TimeEntryMutation =
    | ExistingTimeEntryUpdateMutation
    | ExistingTimeEntryDeleteMutation
    | SuggestionTimeEntryUpdateMutation
    | SuggestionTimeEntryDeleteMutation
    | DraftTimeEntryDeleteMutation
    | DraftTimeEntryCreateMutation
    | SuggestionTimeEntryCreateMutation;

export type Interaction =
    | { kind: "idle" }
    | {
          kind: "move";
          event: ExistingTimeEntryEvent | SuggestionTimeEntryEvent;
          pointerOffsetMs?: number;
          mutation: ExistingTimeEntryUpdateMutation | SuggestionTimeEntryUpdateMutation;
      }
    | {
          kind: "resize";
          event: ExistingTimeEntryEvent | SuggestionTimeEntryEvent;
          mutation: ExistingTimeEntryUpdateMutation | SuggestionTimeEntryUpdateMutation;
      }
    | {
          kind: "draft";
          event: DraftTimeEntryEvent;
          anchorStartMs: number;
      }
    | {
          kind: "create";
          event: DraftTimeEntryEvent | SuggestionTimeEntryEvent;
          mutation: DraftTimeEntryCreateMutation | SuggestionTimeEntryCreateMutation;
      }
    | {
          kind: "edit";
          event: ExistingTimeEntryEvent | SuggestionTimeEntryEvent;
          mutation: ExistingTimeEntryUpdateMutation | SuggestionTimeEntryUpdateMutation;
      }
    | {
          kind: "conflict";
          event: TimeEntryEvent;
          overlaps: TimeEntryEvent[];
          mutation: ExistingTimeEntryUpdateMutation | SuggestionTimeEntryUpdateMutation | DraftTimeEntryCreateMutation | SuggestionTimeEntryCreateMutation;
      };

export function isTimeEntryEvent(e: CalendarEvent): e is TimeEntryEvent {
    return e.kind === "suggestion" || e.kind === "existing" || e.kind === "draft";
}
