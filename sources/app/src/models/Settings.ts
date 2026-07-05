import type { CalendarInterval } from "@/components/tracking/calendar/composables/useCalendarInterval";
import type { OverlayType } from "@/models/DisplaySettings";

export type ListStriped = null | "even" | "odd";

export type CalendarDayRange = {
    startHour: number;
    endHour: number;
};

export type CalendarSettings = {
    intervalMinutes: CalendarInterval;
    dayRange: CalendarDayRange;
};

export type ListSettings = {
    groupByDate: boolean;
    striped: ListStriped;
};

export type GeneralSettings = {
    overlayType: OverlayType;
    theme: string;
};
