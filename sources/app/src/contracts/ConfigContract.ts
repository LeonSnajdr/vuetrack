import type { CalendarInterval } from "@/components/tracking/calendar/composables/useCalendarInterval";

export type CalendarSettingsContract = {
    intervalMinutes: CalendarInterval;
};

export type ListSettingsContract = {
    groupByDate: boolean;
};

export type GeneralSettingsContract = {
    // Reserved for future general settings
};
