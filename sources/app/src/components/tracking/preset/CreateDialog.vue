<template>
    <VDialog v-model="dialogOpen" activator="parent" maxWidth="600">
        <VCard>
            <VCardTitle>
                {{ $t("sidebar.preset.form.create") }}
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
                    {{ $t("action.create") }}
                </VBtn>
            </VCardActions>
        </VCard>
    </VDialog>
</template>

<script setup lang="ts">
import type { TimeEntryPresetCreate } from "@/models/TimeEntryPreset";

const presetStore = usePresetStore();
const { createPreset } = presetStore;

const dialogOpen = ref(false);

const emptyPreset = (): TimeEntryPresetCreate => ({
    name: "",
    taskId: null,
    durationMinutes: null,
    projectId: null,
    activityId: null
});

const draftPreset = ref<TimeEntryPresetCreate>(emptyPreset());

const canSavePreset = computed(() => draftPreset.value.name.trim().length > 0);

const submit = (): void => {
    if (!canSavePreset.value) return;

    createPreset({
        name: draftPreset.value.name.trim(),
        taskId: draftPreset.value.taskId?.trim() || null,
        durationMinutes: draftPreset.value.durationMinutes,
        projectId: draftPreset.value.projectId,
        activityId: draftPreset.value.activityId
    });

    dialogOpen.value = false;
};

watch(dialogOpen, (isOpen) => {
    if (!isOpen) {
        draftPreset.value = emptyPreset();
    }
});
</script>
