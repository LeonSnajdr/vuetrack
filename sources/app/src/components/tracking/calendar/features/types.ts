import type { TimeEntryEvent } from "@/components/tracking/calendar/types";

export interface ConflictResolutionStrategy {
    id: string;
    label: string;
    subtitle: string;
    icon: string;
    variant?: "error" | "warning";
    resolve: (ctx: ConflictContext) => ConflictResolutionResult | null;
}

export interface ConflictContext {
    event: TimeEntryEvent;
    overlaps: TimeEntryEvent[];
    allEvents: TimeEntryEvent[];
}

export interface ConflictResolutionResult {
    position: { start: number; end: number };
    mutations?: EventMutation[];
}

export type EventMutation = { action: "remove"; event: TimeEntryEvent } | { action: "update"; event: TimeEntryEvent; start: number; end: number };
