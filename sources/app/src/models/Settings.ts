import type { CalendarInterval } from "@/components/tracking/calendar/composables/useCalendarInterval";
import type { OverlayType } from "@/models/DisplaySettings";

export type ListStriped = null | "even" | "odd";

export type CalendarSettings = {
    intervalMinutes: CalendarInterval;
};

export type ListSettings = {
    groupByDate: boolean;
    striped: ListStriped;
};

export type GeneralSettings = {
    overlayType: OverlayType;
    theme: string;
};
