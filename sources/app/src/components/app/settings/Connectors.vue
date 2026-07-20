<template>
    <div class="h-100 d-flex flex-column ga-4">
        <VCard v-for="connector in connectors" :key="connector.key" class="border" variant="outlined">
            <VCardTitle>
                <VIcon :icon="iconMap[connector.key]" />
                {{ nameFor(connector.key) }}
                <VSpacer />
                <VChip :color="chipFor(connector.key).color" variant="tonal">
                    <template #prepend>
                        <VIcon :color="chipFor(connector.key).color" :icon="chipFor(connector.key).icon" class="mr-2" />
                    </template>
                    {{ chipFor(connector.key).text }}
                </VChip>
            </VCardTitle>
            <VCardText>
                <template v-if="isOAuth(connector)">
                    <div v-if="statusFor(connector.key).connected" class="d-flex ga-2">
                        <VBtn
                            v-if="!statusFor(connector.key).healthy"
                            @click="connect(connector.key)"
                            :loading="isConnecting(connector.key)"
                            :prependIcon="iconMap[connector.key]"
                            color="primary"
                            variant="flat"
                        >
                            {{ $t("settings.connectors.reconnect", { name: nameFor(connector.key) }) }}
                        </VBtn>
                        <VBtn
                            @click="disconnect(connector.key)"
                            :loading="isDisconnecting(connector.key)"
                            :prependIcon="mdiLinkOff"
                            color="error"
                            variant="tonal"
                        >
                            {{ $t("settings.connectors.disconnect") }}
                        </VBtn>
                    </div>
                    <VBtn
                        v-else
                        @click="connect(connector.key)"
                        :loading="isConnecting(connector.key)"
                        :prependIcon="iconMap[connector.key]"
                        color="primary"
                        variant="flat"
                    >
                        {{ $t("settings.connectors.connect", { name: nameFor(connector.key) }) }}
                    </VBtn>
                </template>
                <span v-else class="text-medium-emphasis">{{ nameFor(connector.key) }}</span>
            </VCardText>
        </VCard>
    </div>
</template>

<script setup lang="ts">
const { t } = useI18n();

const connectorStore = useConnectorStore();
const { connectors } = storeToRefs(connectorStore);
const { statusFor, isConnecting, isDisconnecting, isOAuth, nameFor, connect, disconnect } = connectorStore;

const iconMap: Record<string, string> = { jira: mdiJira, github: mdiGithub };

const chipFor = (key: string) => {
    const status = statusFor(key);
    if (status.connected && status.healthy) {
        return { color: "success", icon: mdiCircle, text: t("settings.connectors.connected") };
    }
    if (status.connected) {
        return { color: "warning", icon: mdiAlert, text: t("settings.connectors.reconnectRequired") };
    }
    return { color: "", icon: mdiCircleOutline, text: t("settings.connectors.notConnected") };
};

onBeforeUnmount(connectorStore.cancelPending);
</script>
