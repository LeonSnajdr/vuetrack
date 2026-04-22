<template>
    <div class="d-flex flex-column ga-4">
        <div class="d-flex align-center justify-space-between ga-2">
            <div>{{ $t("preset.plural") }}</div>
            <VIconBtn v-tooltip="$t('action.create')">
                <VIcon :icon="mdiPlus" size="small" />
                <TrackingPresetCreateDialog />
            </VIconBtn>
        </div>
        <VAlert v-if="!presets.length" density="compact" type="info" variant="tonal">
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
                >
                    <span class="text-truncate mr-2">{{ preset.name }}</span>
                    <template #append>
                        <VIconBtn size="x-small" variant="text">
                            <VIcon :icon="mdiPencil" size="small" />
                            <TrackingPresetEditDialog :preset="preset" />
                        </VIconBtn>
                    </template>
                </VChip>
            </VChipGroup>
        </template>
    </div>
</template>

<script setup lang="ts">
const presetStore = usePresetStore();
const { presets, activePresetId } = storeToRefs(presetStore);
</script>
