import type { CalendarSettingsContract, GeneralSettingsContract, ListSettingsContract } from "@/contracts/ConfigContract";

export const useConfigStore = defineStore(
    "config",
    () => {
        const calendarConfig = ref<CalendarSettingsContract>({
            intervalMinutes: 30
        });

        const listConfig = ref<ListSettingsContract>({
            groupByDate: false
        });

        const generalConfig = ref<GeneralSettingsContract>({});

        return {
            calendarConfig,
            listConfig,
            generalConfig
        };
    },
    {
        persist: true
    }
);
