import type { CalendarInterval } from "@/components/tracking/calendar/composables/useCalendarInterval";

export type CalendarSettings = {
    intervalMinutes: CalendarInterval;
};

export type ListSettings = {
    groupByDate: boolean;
};

export type GeneralSettings = {
    // Reserved for future general settings
};
