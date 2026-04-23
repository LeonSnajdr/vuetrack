<template>
    <VCard elevation="0">
        <VCardTitle class="text-title-medium">
            {{ $t("preset.plural") }}
            <VSpacer />
            <VIconBtn size="small" v-tooltip="$t('action.create')">
                <VIcon :icon="mdiPlus" size="small" />
                <TrackingPresetCreateDialog />
            </VIconBtn>
        </VCardTitle>
        <VCardText>
            <VAlert v-if="!presets.length" class="text-body-small text-medium-emphasis" density="compact" variant="tonal">
                {{ $t("preset.empty") }}
            </VAlert>
            <template v-else>
                <VChipGroup :modelValue="activePresetId ?? undefined" column>
                    <VChip
                        v-for="preset in presets"
                        :key="preset.id"
                        @click="presetStore.toggleActivePreset(preset.id)"
                        :value="preset.id"
                        :variant="activePresetId === preset.id ? 'flat' : 'tonal'"
                        density="comfortable"
                    >
                        <span class="text-truncate mr-2">{{ preset.name }}</span>
                        <template #append>
                            <VIconBtn class="mx-n1" size="x-small" variant="text">
                                <VIcon :icon="mdiPencil" size="small" />
                                <TrackingPresetEditDialog :preset="preset" />
                            </VIconBtn>
                        </template>
                    </VChip>
                </VChipGroup>
            </template>
        </VCardText>
    </VCard>
</template>

<script setup lang="ts">
const presetStore = usePresetStore();
const { presets, activePresetId } = storeToRefs(presetStore);
</script>
