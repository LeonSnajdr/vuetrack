import type { TimeEntryCreateContract, TimeEntryContract, TimeEntryUpdateContract, TimeEntryId } from "@/contracts/TimeEntryContract";
import type { TimeEntrySuggestionContract, TimeEntrySuggestionUpdateContract } from "@/contracts/TimeEntrySuggestion";
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
    eventKind: "existing";
    update: TimeEntryUpdateContract;
    originalPosition: EventPosition;
};

export type ExistingTimeEntryDeleteMutation = {
    kind: "delete";
    eventKind: "existing";
    id: TimeEntryId;
};

export type SuggestionTimeEntryEvent = {
    kind: "suggestion";
    timeEntry: TimeEntrySuggestionContract;
} & BaseCalendarEvent;

export type SuggestionTimeEntryUpdateMutation = {
    kind: "update";
    eventKind: "suggestion";
    update: TimeEntrySuggestionUpdateContract;
    originalPosition: EventPosition;
};

export type SuggestionTimeEntryDeleteMutation = {
    kind: "delete";
    eventKind: "suggestion";
    id: TimeEntryId;
};

export type TimeEntryEvent = DraftTimeEntryEvent | ExistingTimeEntryEvent | SuggestionTimeEntryEvent;
export type TimeEntryMutation =
    | ExistingTimeEntryUpdateMutation
    | ExistingTimeEntryDeleteMutation
    | SuggestionTimeEntryUpdateMutation
    | SuggestionTimeEntryDeleteMutation;

export type Interaction =
    | { kind: "idle" }
    | {
          kind: "move";
          event: TimeEntryEvent;
          pointerOffsetMs?: number;
          originalStartMs: number;
          originalEndMs: number;
      }
    | {
          kind: "resize";
          event: TimeEntryEvent;
          originalEndMs: number;
      }
    | {
          kind: "draft";
          event: DraftTimeEntryEvent;
          anchorStartMs: number;
      }
    | { kind: "create"; event: DraftTimeEntryEvent | SuggestionTimeEntryEvent }
    | { kind: "edit"; event: ExistingTimeEntryEvent | SuggestionTimeEntryEvent }
    | {
          kind: "conflict";
          event: TimeEntryEvent;
          overlaps: TimeEntryEvent[];
          onResolved: (position: { start: number; end: number }) => Promise<void>;
          onCanceled: () => Promise<void>;
      };

export function isTimeEntryEvent(e: CalendarEvent): e is TimeEntryEvent {
    return e.kind === "suggestion" || e.kind === "existing" || e.kind === "draft";
}
