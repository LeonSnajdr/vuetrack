<template>
    <VDialog v-model="dialogOpen" activator="parent" maxWidth="600">
        <VCard>
            <VCardTitle>
                {{ $t("action.create.title", { type: $t("preset.singular") }) }}
            </VCardTitle>
            <VCardText>
                <VForm v-model="valid">
                    <TrackingPresetContainer v-model="draftPreset" />
                </VForm>
            </VCardText>
            <VCardActions>
                <VSpacer />
                <VBtn @click="dialogOpen = false" variant="text">
                    {{ $t("action.cancel") }}
                </VBtn>
                <VBtn @click="submit" :disabled="!valid" color="primary" variant="flat">
                    {{ $t("action.create") }}
                </VBtn>
            </VCardActions>
        </VCard>
    </VDialog>
</template>

<script setup lang="ts">
import type { TimeEntryPresetCreate } from "@/models/TimeEntryPreset";

const presetStore = usePresetStore();

const dialogOpen = ref(false);
const valid = ref(false);

const emptyPreset = (): Nullable<TimeEntryPresetCreate> => ({
    name: null,
    taskId: null,
    durationMinutes: null,
    projectId: null,
    activityId: null
});

const draftPreset = ref<Nullable<TimeEntryPresetCreate>>(emptyPreset());

const submit = (): void => {
    if (!valid.value) return;
    presetStore.createPreset(draftPreset.value);
    dialogOpen.value = false;
};

whenever(dialogOpen, () => {
    draftPreset.value = emptyPreset();
});
</script>
