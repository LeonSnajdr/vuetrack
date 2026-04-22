import type { TimeEntryPreset, TimeEntryPresetId, TimeEntryPresetCreate } from "@/models/TimeEntryPreset";

export const usePresetStore = defineStore("preset", () => {
    const presets = ref<TimeEntryPreset[]>([]);
    const activePresetId = ref<TimeEntryPresetId | null>(null);

    const activePreset = computed<TimeEntryPreset | null>(() => presets.value.find((preset) => preset.id === activePresetId.value) ?? null);

    const createPreset = (preset: TimeEntryPresetCreate): TimeEntryPreset => {
        const createdPreset: TimeEntryPreset = {
            id: uuidv4() as TimeEntryPresetId,
            ...preset
        };

        presets.value.push(createdPreset);

        return createdPreset;
    };

    const updatePreset = (id: TimeEntryPresetId, preset: TimeEntryPresetCreate): void => {
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
});
