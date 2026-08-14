<template>
    <div class="h-100 d-flex flex-column ga-4">
        <VCard class="border" variant="outlined">
            <VCardTitle>
                <VIcon :icon="mdiFormatListGroup" />
                {{ $t("settings.list.groupByDate") }}
            </VCardTitle>
            <VCardText>
                <VBtnToggle v-model="listSettings.groupByDate" mandatory>
                    <VBtn :value="false">{{ $t("settings.list.groupByDate.off") }}</VBtn>
                    <VBtn :value="true">{{ $t("settings.list.groupByDate.on") }}</VBtn>
                </VBtnToggle>
            </VCardText>
        </VCard>
        <VCard class="border" variant="outlined">
            <VCardTitle>
                <VIcon :icon="mdiFormatLineStyle" />
                {{ $t("settings.list.striped") }}
            </VCardTitle>
            <VCardText>
                <VBtnToggle v-model="listSettings.striped" mandatory>
                    <VBtn :value="null">{{ $t("settings.list.striped.none") }}</VBtn>
                    <VBtn value="even">{{ $t("settings.list.striped.even") }}</VBtn>
                    <VBtn value="odd">{{ $t("settings.list.striped.odd") }}</VBtn>
                </VBtnToggle>
            </VCardText>
        </VCard>
    </div>
</template>

<script setup lang="ts">
const settingsStore = useSettingsStore();
const { listSettings } = storeToRefs(settingsStore);
</script>
