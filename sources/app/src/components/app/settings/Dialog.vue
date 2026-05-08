<template>
    <VDialog v-model="open" minHeight="600" width="900">
        <VCard>
            <VCardTitle>{{ $t("settings.title") }}</VCardTitle>
            <VCardText class="d-flex h-100 overflow-auto ga-4">
                <VTabs v-model="activeTab" direction="vertical" style="min-width: 150px">
                    <VTab :prependIcon="mdiCog" value="general">{{ $t("settings.general") }}</VTab>
                    <VTab :prependIcon="mdiCalendar" value="calendar">{{ $t("settings.calendar") }}</VTab>
                    <VTab :prependIcon="mdiFormatListBulleted" value="list">{{ $t("settings.list") }}</VTab>
                </VTabs>
                <VTabsWindow v-model="activeTab" class="flex-grow-1">
                    <VTabsWindowItem value="general">
                        <AppSettingsGeneral />
                    </VTabsWindowItem>
                    <VTabsWindowItem value="calendar">
                        <AppSettingsCalendar />
                    </VTabsWindowItem>
                    <VTabsWindowItem value="list">
                        <AppSettingsList />
                    </VTabsWindowItem>
                </VTabsWindow>
            </VCardText>
            <VCardActions>
                <VSpacer />
                <VBtn @click="open = false" variant="flat">{{ $t("action.close") }}</VBtn>
            </VCardActions>
        </VCard>
    </VDialog>
</template>

<script setup lang="ts">
const open = defineModel<boolean>({ default: false });
const activeTab = ref("general");
</script>
