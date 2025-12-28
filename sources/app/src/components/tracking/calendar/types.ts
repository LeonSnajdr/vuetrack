import type { TimeEntryCreateContract, TimeEntryContract } from "@/contracts/TimeEntryContract";
import type { TimeEntrySuggestionContract } from "@/contracts/TimeEntrySuggestion";
import type { CalendarEvent } from "vuetify/lib/components/VCalendar/types.mjs";

export type BaseCalendarEvent = {
    uiId: string;
    timed: boolean;
    color: string;
    start: number;
    end: number;
};

export type TimeEntryEvent =
    | ({ kind: "draft"; createEntry: TimeEntryCreateContract } & BaseCalendarEvent)
    | ({ kind: "existing"; timeEntry: TimeEntryContract } & BaseCalendarEvent)
    | ({ kind: "suggestion"; timeEntry: TimeEntrySuggestionContract } & BaseCalendarEvent);

export type DraftTimeEntryEvent = Extract<TimeEntryEvent, { kind: "draft" }>;
export type ExistingTimeEntryEvent = Extract<TimeEntryEvent, { kind: "existing" }>;
export type SuggestionTimeEntryEvent = Extract<TimeEntryEvent, { kind: "suggestion" }>;

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
