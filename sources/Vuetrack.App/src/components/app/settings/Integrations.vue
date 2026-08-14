<template>
    <div class="h-100 d-flex flex-column ga-4">
        <VCard v-for="integration in integrations" :key="integration.key" class="border" variant="outlined">
            <VCardTitle>
                <VIcon :icon="iconMap[integration.key]" />
                {{ nameFor(integration.key) }}
                <VSpacer />
                <VChip :color="chipFor(integration.key).color" variant="tonal">
                    <template #prepend>
                        <VIcon :color="chipFor(integration.key).color" :icon="chipFor(integration.key).icon" class="mr-2" />
                    </template>
                    {{ chipFor(integration.key).text }}
                </VChip>
            </VCardTitle>
            <VCardText>
                <div v-if="statusFor(integration.key).connected" class="d-flex ga-2">
                    <VBtn
                        v-if="!statusFor(integration.key).healthy"
                        @click="connect(integration.key)"
                        :loading="isConnecting(integration.key)"
                        :prependIcon="iconMap[integration.key]"
                        color="primary"
                        variant="flat"
                    >
                        {{ $t("settings.integration.reconnect", { name: nameFor(integration.key) }) }}
                    </VBtn>
                    <VBtn
                        @click="disconnect(integration.key)"
                        :loading="isDisconnecting(integration.key)"
                        :prependIcon="mdiLinkOff"
                        color="error"
                        variant="tonal"
                    >
                        {{ $t("settings.integration.disconnect") }}
                    </VBtn>
                </div>
                <VBtn
                    v-else
                    @click="connect(integration.key)"
                    :loading="isConnecting(integration.key)"
                    :prependIcon="iconMap[integration.key]"
                    color="primary"
                    variant="flat"
                >
                    {{ $t("settings.integration.connect", { name: nameFor(integration.key) }) }}
                </VBtn>
            </VCardText>
        </VCard>
    </div>
</template>

<script setup lang="ts">
import { IntegrationKey } from "@/contracts/IntegrationContract";

const props = defineProps<{ keys: IntegrationKey[] }>();

const { t } = useI18n();

const integrationStore = useIntegrationStore();
const { statusFor, isConnecting, isDisconnecting, nameFor, connect, disconnect } = integrationStore;

const integrations = integrationStore.listFor(props.keys);

const iconMap: Record<string, string> = {
    [IntegrationKey.Jira]: mdiJira,
    [IntegrationKey.Github]: mdiGithub,
    [IntegrationKey.Timetracking]: mdiClockOutline
};

const chipFor = (key: IntegrationKey) => {
    const status = statusFor(key);
    if (status.connected && status.healthy) {
        return { color: "success", icon: mdiCircle, text: t("settings.integration.connected") };
    }
    if (status.connected) {
        return { color: "warning", icon: mdiAlert, text: t("settings.integration.reconnectRequired") };
    }
    return { color: "", icon: mdiCircleOutline, text: t("settings.integration.notConnected") };
};

onBeforeUnmount(integrationStore.cancelPending);
</script>
