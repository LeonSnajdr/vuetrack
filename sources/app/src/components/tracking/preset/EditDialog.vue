<template>
    <VDialog v-model="dialogOpen" activator="parent" maxWidth="600">
        <VCard>
            <VCardTitle>
                {{ $t("sidebar.preset.form.edit") }}
            </VCardTitle>
            <VCardText>
                <TrackingPresetContainer v-model="draftPreset" />
            </VCardText>
            <VCardActions>
                <VSpacer />
                <VBtn @click="dialogOpen = false" variant="text">
                    {{ $t("action.cancel") }}
                </VBtn>
                <VBtn @click="submit" :disabled="!canSavePreset" color="primary" variant="flat">
                    {{ $t("action.save") }}
                </VBtn>
            </VCardActions>
        </VCard>
    </VDialog>
</template>

<script setup lang="ts">
import type { TimeEntryPreset, TimeEntryPresetCreate } from "@/models/TimeEntryPreset";

const { preset } = defineProps<{
    preset: TimeEntryPreset;
}>();

const presetStore = usePresetStore();
const { updatePreset } = presetStore;

const dialogOpen = ref(false);
const draftPreset = ref<TimeEntryPresetCreate>({
    name: "",
    taskId: null,
    durationMinutes: null,
    projectId: null,
    activityId: null
});

const canSavePreset = computed(() => draftPreset.value.name.trim().length > 0);

const syncDraft = (): void => {
    draftPreset.value = {
        name: preset.name,
        taskId: preset.taskId,
        durationMinutes: preset.durationMinutes,
        projectId: preset.projectId,
        activityId: preset.activityId
    };
};

const submit = (): void => {
    if (!canSavePreset.value) return;

    updatePreset(preset.id, {
        name: draftPreset.value.name.trim(),
        taskId: draftPreset.value.taskId?.trim() || null,
        durationMinutes: draftPreset.value.durationMinutes,
        projectId: draftPreset.value.projectId,
        activityId: draftPreset.value.activityId
    });

    dialogOpen.value = false;
};

watch(dialogOpen, (isOpen) => {
    if (isOpen) {
        syncDraft();
    }
});
</script>
