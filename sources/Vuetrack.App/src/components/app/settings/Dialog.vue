<template>
    <VDialog v-model="isOpen" height="600" width="900">
        <VCard>
            <VCardTitle>{{ $t("settings.title") }}</VCardTitle>
            <VCardText class="d-flex h-100 overflow-auto ga-4">
                <VTabs v-model="activeTab" direction="vertical" style="min-width: 150px">
                    <VTab :prependIcon="mdiCog" value="general">{{ $t("settings.general") }}</VTab>
                    <VTab :prependIcon="mdiCalendar" value="calendar">{{ $t("settings.calendar") }}</VTab>
                    <VTab :prependIcon="mdiFormatListBulleted" value="list">{{ $t("settings.list") }}</VTab>
                    <VTab :prependIcon="mdiConnection" value="connectors">{{ $t("settings.connectors") }}</VTab>
                    <VTab :prependIcon="mdiClockOutline" value="backends">{{ $t("settings.backends") }}</VTab>
                </VTabs>
                <VTabsWindow v-model="activeTab" class="flex-grow-1 overflow-auto overflow-x-hidden">
                    <VTabsWindowItem value="general">
                        <AppSettingsGeneral />
                    </VTabsWindowItem>
                    <VTabsWindowItem value="calendar">
                        <AppSettingsCalendar />
                    </VTabsWindowItem>
                    <VTabsWindowItem value="list">
                        <AppSettingsList />
                    </VTabsWindowItem>
                    <VTabsWindowItem value="connectors">
                        <AppSettingsIntegrations :keys="connectorKeys" />
                    </VTabsWindowItem>
                    <VTabsWindowItem value="backends">
                        <AppSettingsIntegrations :keys="backendKeys" />
                    </VTabsWindowItem>
                </VTabsWindow>
            </VCardText>
            <VCardActions>
                <VSpacer />
                <VBtn @click="isOpen = false" variant="flat">{{ $t("action.close") }}</VBtn>
            </VCardActions>
        </VCard>
    </VDialog>
</template>

<script setup lang="ts">
import { IntegrationKey } from "@/contracts/IntegrationContract";

const settingsDialogStore = useSettingsDialogStore();
const { isOpen, activeTab } = storeToRefs(settingsDialogStore);

const connectorKeys = [IntegrationKey.Jira, IntegrationKey.Github];
const backendKeys = [IntegrationKey.Timetracking];
</script>
