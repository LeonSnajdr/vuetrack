<template>
    <VDialog v-model="dialogOpen" activator="parent" maxWidth="600">
        <VCard>
            <VCardTitle>
                {{ $t("action.save.title", { type: $t("preset.singular") }) }}
            </VCardTitle>
            <VCardText>
                <VForm v-model="valid">
                    <TrackingPresetContainer v-model="draftPreset" />
                </VForm>
            </VCardText>
            <VCardActions>
                <VBtn :prependIcon="mdiDelete" variant="plain">
                    <BaseConfirmationDialog @confirm="remove" :type="$t('preset.singular')" activator="parent" />
                    {{ $t("action.delete") }}
                </VBtn>
                <VSpacer />
                <VBtn @click="dialogOpen = false" variant="text">
                    {{ $t("action.cancel") }}
                </VBtn>
                <VBtn @click="submit" :disabled="!valid" color="primary" variant="flat">
                    {{ $t("action.save") }}
                </VBtn>
            </VCardActions>
        </VCard>
    </VDialog>
</template>

<script setup lang="ts">
import { useClonedMapped } from "@/composables/useClonedMapped";
import type { TimeEntryPreset } from "@/models/TimeEntryPreset";

const props = defineProps<{
    preset: TimeEntryPreset;
}>();

const presetStore = usePresetStore();

const dialogOpen = ref(false);
const valid = ref(false);

const { cloned: draftPreset } = useClonedMapped(
    () => props.preset,
    (x) => ({
        name: x.name,
        taskId: x.taskId,
        durationMinutes: x.durationMinutes,
        projectId: x.projectId,
        activityId: x.activityId
    })
);

const submit = (): void => {
    if (!valid.value) return;
    presetStore.updatePreset(props.preset.id, draftPreset.value);
    dialogOpen.value = false;
};

const remove = (): void => {
    presetStore.deletePreset(props.preset.id);
    dialogOpen.value = false;
};
</script>
