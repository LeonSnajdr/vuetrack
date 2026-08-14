<template>
    <div class="h-100 d-flex flex-column ga-4">
        <AppSettingsTheme v-model="generalSettings.theme" />
        <VCard class="border" variant="outlined">
            <VCardTitle>
                <VIcon :icon="mdiTune" />
                {{ $t("settings.general.overlayType") }}
            </VCardTitle>
            <VCardText>
                <VBtnToggle v-model="generalSettings.overlayType" mandatory>
                    <VBtn :value="OverlayType.Menu">{{ $t("settings.general.overlayType.menu") }}</VBtn>
                    <VBtn :value="OverlayType.Dialog">{{ $t("settings.general.overlayType.dialog") }}</VBtn>
                    <VBtn :value="OverlayType.Drawer">{{ $t("settings.general.overlayType.drawer") }}</VBtn>
                </VBtnToggle>
            </VCardText>
        </VCard>
    </div>
</template>

<script setup lang="ts">
import { OverlayType } from "@/models/DisplaySettings";

const settingsStore = useSettingsStore();
const { generalSettings } = storeToRefs(settingsStore);
</script>
