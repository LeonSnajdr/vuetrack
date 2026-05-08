import type { CalendarSettings, GeneralSettings, ListSettings } from "@/models/Settings";
import { OverlayType } from "@/models/DisplaySettings";

export const useSettingsStore = defineStore(
    "settings",
    () => {
        const calendarSettings = ref<CalendarSettings>({
            intervalMinutes: 30
        });

        const listSettings = ref<ListSettings>({
            groupByDate: false,
            striped: null
        });

        const generalSettings = ref<GeneralSettings>({
            overlayType: OverlayType.Drawer,
            theme: "system"
        });

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
