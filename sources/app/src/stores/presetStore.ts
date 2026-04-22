import type { TimeEntryPreset, TimeEntryPresetId, TimeEntryPresetCreate, TimeEntryPresetUpdate } from "@/models/TimeEntryPreset";
import { isNonNullable, type Nullable } from "@/util/Nullable";

export const usePresetStore = defineStore(
    "preset",
    () => {
        const presets = ref<TimeEntryPreset[]>([]);
        const activePresetId = ref<TimeEntryPresetId | null>(null);

        const activePreset = computed<TimeEntryPreset | null>(() => presets.value.find((preset) => preset.id === activePresetId.value) ?? null);

        const createPreset = (preset: Nullable<TimeEntryPresetCreate>): TimeEntryPreset | undefined => {
            if (!isNonNullable(preset)) return;

            const createdPreset: TimeEntryPreset = {
                id: uuidv4() as TimeEntryPresetId,
                ...preset
            };

            presets.value.push(createdPreset);

            return createdPreset;
        };

        const updatePreset = (id: TimeEntryPresetId, preset: TimeEntryPresetUpdate): void => {
            const index = presets.value.findIndex((item) => item.id === id);
            if (index < 0) return;

            presets.value[index] = {
                id,
                ...preset
            };
        };

        const setActivePreset = (id: TimeEntryPresetId | null): void => {
            activePresetId.value = id;
        };

        const toggleActivePreset = (id: TimeEntryPresetId): void => {
            activePresetId.value = activePresetId.value === id ? null : id;
        };

        return {
            presets,
            activePresetId,
            activePreset,
            createPreset,
            updatePreset,
            setActivePreset,
            toggleActivePreset
        };
    },
    {
        persist: true
    }
);
