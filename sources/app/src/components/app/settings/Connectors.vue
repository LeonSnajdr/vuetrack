<template>
    <div class="h-100 d-flex flex-column ga-4">
        <p class="text-medium-emphasis">{{ $t("settings.connectors.description") }}</p>

        <VCard v-for="connector in connectors" :key="connector.key" class="border" variant="outlined">
            <VCardTitle>
                <VIcon :icon="iconFor(connector.key)" />
                {{ connector.displayName }}
            </VCardTitle>
            <VCardText>
                <template v-if="isOAuth(connector)">
                    <template v-if="statusFor(connector.key).connected">
                        <VAlert v-if="statusFor(connector.key).healthy" :icon="mdiCheckCircle" type="success" variant="tonal">
                            {{ $t("settings.connectors.connected") }}
                        </VAlert>
                        <VAlert v-else :icon="mdiAlert" type="warning" variant="tonal">
                            {{ $t("settings.connectors.reconnectRequired", { name: connector.displayName }) }}
                        </VAlert>
                        <div class="mt-3 d-flex ga-2">
                            <VBtn
                                v-if="!statusFor(connector.key).healthy"
                                @click="connect(connector)"
                                :loading="isConnecting(connector.key)"
                                :prependIcon="iconFor(connector.key)"
                                color="primary"
                                variant="flat"
                            >
                                {{ $t("settings.connectors.reconnect", { name: connector.displayName }) }}
                            </VBtn>
                            <VBtn @click="disconnect(connector)" :loading="isDisconnecting(connector.key)" :prependIcon="mdiLinkOff" color="error" variant="tonal">
                                {{ $t("settings.connectors.disconnect") }}
                            </VBtn>
                        </div>
                    </template>
                    <template v-else>
                        <VBtn @click="connect(connector)" :loading="isConnecting(connector.key)" :prependIcon="iconFor(connector.key)" color="primary" variant="flat">
                            {{ $t("settings.connectors.connect", { name: connector.displayName }) }}
                        </VBtn>
                    </template>
                    <VAlert v-if="errorFor(connector.key)" class="mt-3" type="error" variant="tonal">{{ errorFor(connector.key) }}</VAlert>
                </template>
                <span v-else class="text-medium-emphasis">{{ connector.key }}</span>
            </VCardText>
        </VCard>
    </div>
</template>

<script setup lang="ts">
import type { ConnectorDescriptorContract, ConnectorStatusResponse } from "@/contracts/ConnectorContract";

const { t } = useI18n();

const connectorStore = useConnectorStore();
const { connectors, statuses } = storeToRefs(connectorStore);

const connectingKeys = ref<Record<string, boolean>>({});
const disconnectingKeys = ref<Record<string, boolean>>({});
const errors = ref<Record<string, string>>({});

const defaultStatus: ConnectorStatusResponse = { connected: false, healthy: false };
const statusFor = (key: string): ConnectorStatusResponse => statuses.value[key] ?? defaultStatus;
const isConnecting = (key: string): boolean => connectingKeys.value[key] ?? false;
const isDisconnecting = (key: string): boolean => disconnectingKeys.value[key] ?? false;
const errorFor = (key: string): string => errors.value[key] ?? "";

const isOAuth = (connector: ConnectorDescriptorContract): boolean => connector.capabilities?.toLowerCase().includes("oauth") ?? false;

const iconMap: Record<string, string> = { jira: mdiJira, github: mdiGithub };
const iconFor = (key: string): string => iconMap[key] ?? mdiConnection;

const nameFor = (key: string): string => connectors.value.find((c) => c.key === key)?.displayName ?? key;

const errorMessage = (e: unknown, key: string): string => {
    const responseErrors = (e as { response?: { data?: { errors?: unknown } } })?.response?.data?.errors;
    if (Array.isArray(responseErrors) && responseErrors.length) return responseErrors.join(" ");
    return t("settings.connectors.error", { name: nameFor(key) });
};

onBeforeMount(async () => {
    await connectorStore.executeLoad();
    const oauthConnectors = connectors.value.filter(isOAuth);
    await Promise.all(oauthConnectors.map((connector) => connectorStore.loadStatus(connector.key)));
});

// Popup OAuth: the settings dialog stays open; the consent runs in a popup that relays the
// result back via postMessage from the static /{key}-callback.html relay page.
let activePopup: Window | null = null;
let activeListener: ((event: MessageEvent) => void) | null = null;
let closedTimer = 0;

const stopListening = () => {
    if (activeListener) {
        window.removeEventListener("message", activeListener);
        activeListener = null;
    }
    if (closedTimer) {
        window.clearInterval(closedTimer);
        closedTimer = 0;
    }
};

const handleResult = async (key: string, data: { code?: string; state?: string; error?: string }, expectedState: string, redirectUri: string) => {
    stopListening();
    activePopup?.close();
    activePopup = null;

    try {
        if (data.error || !data.code || !data.state || data.state !== expectedState) {
            errors.value[key] = t("settings.connectors.error", { name: nameFor(key) });
            return;
        }

        await ConnectorService.connect(key, { code: data.code, state: data.state, redirectUri });
        await connectorStore.loadStatus(key);
    } catch (e) {
        console.error(`${key} callback failed`, e);
        errors.value[key] = errorMessage(e, key);
    } finally {
        connectingKeys.value[key] = false;
    }
};

const connect = async (connector: ConnectorDescriptorContract) => {
    const key = connector.key;
    errors.value[key] = "";
    connectingKeys.value[key] = true;

    try {
        const redirectUri = `${window.location.origin}/${key}-callback.html`;
        const { authorizationUrl, state } = await ConnectorService.authorize(key, redirectUri);

        activePopup = window.open(authorizationUrl, `${key}-oauth`, "width=600,height=800");
        if (!activePopup) {
            errors.value[key] = t("settings.connectors.popupBlocked");
            connectingKeys.value[key] = false;
            return;
        }

        activeListener = (event: MessageEvent) => {
            if (event.origin !== window.location.origin || event.data?.source !== `${key}-oauth`) return;
            void handleResult(key, event.data, state, redirectUri);
        };
        window.addEventListener("message", activeListener);

        // Stop the spinner if the user closes the popup without finishing.
        closedTimer = window.setInterval(() => {
            if (activePopup?.closed) {
                stopListening();
                connectingKeys.value[key] = false;
            }
        }, 500);
    } catch (e) {
        console.error(`${key} authorize failed`, e);
        errors.value[key] = t("settings.connectors.error", { name: connector.displayName });
        connectingKeys.value[key] = false;
    }
};

const disconnect = async (connector: ConnectorDescriptorContract) => {
    const key = connector.key;
    errors.value[key] = "";
    disconnectingKeys.value[key] = true;

    try {
        await ConnectorService.disconnect(key);
        await connectorStore.loadStatus(key);
    } catch (e) {
        console.error(`${key} disconnect failed`, e);
        errors.value[key] = errorMessage(e, key);
    } finally {
        disconnectingKeys.value[key] = false;
    }
};

onBeforeUnmount(stopListening);
</script>
