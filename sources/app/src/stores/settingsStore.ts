import type { CalendarSettings, GeneralSettings, ListSettings } from "@/models/Settings";

export const useSettingsStore = defineStore(
    "settings",
    () => {
        const calendarSettings = ref<CalendarSettings>({
            intervalMinutes: 30
        });

        const listSettings = ref<ListSettings>({
            groupByDate: false
        });

        const generalSettings = ref<GeneralSettings>({});

        return {
            calendarSettings,
            listSettings,
            generalSettings
        };
    },
    {
        persist: true
    }
);
