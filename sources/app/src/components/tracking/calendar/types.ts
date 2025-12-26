import type { TimeEntryCreateContract, TimeEntryContract } from "@/contracts/TimeEntryContract";
import type { TimeEntrySuggestionContract } from "@/contracts/TimeEntrySuggestion";

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
export type ExistingOrSuggestionTimeEntryEvent = Extract<TimeEntryEvent, { kind: "existing" | "suggestion" }>;

export type Interaction =
    | { kind: "idle" }
    | {
          kind: "move";
          event: ExistingOrSuggestionTimeEntryEvent;
          pointerOffsetMs?: number;
          originalStartMs: number;
          originalEndMs: number;
      }
    | {
          kind: "resize";
          event: ExistingOrSuggestionTimeEntryEvent;
          originalEndMs: number;
      }
    | {
          kind: "draft";
          event: DraftTimeEntryEvent;
          anchorStartMs: number;
          minStartMs: number;
          maxEndMs: number;
      }
    | { kind: "create"; event: DraftTimeEntryEvent }
    | {
          kind: "conflict";
          event: ExistingOrSuggestionTimeEntryEvent;
          originalStartMs: number;
          originalEndMs: number;
          overlaps: TimeEntryEvent[];
      };
