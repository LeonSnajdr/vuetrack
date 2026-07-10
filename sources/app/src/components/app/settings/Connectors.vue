<template>
    <div class="h-100 d-flex flex-column ga-4">
        <p class="text-medium-emphasis">{{ $t("settings.connectors.description") }}</p>

        <VCard v-for="connector in connectors" :key="connector.key" class="border" variant="outlined">
            <VCardTitle>
                <VIcon :icon="iconFor(connector.key)" />
                {{ connector.displayName }}
            </VCardTitle>
            <VCardText>
                <template v-if="connector.key === 'jira'">
                    <template v-if="jiraStatus.connected">
                        <VAlert :icon="mdiCheckCircle" type="success" variant="tonal">
                            {{ $t("settings.connectors.jira.connected") }}
                            <template v-if="jiraStatus.siteUrl"> — {{ jiraStatus.siteUrl }}</template>
                        </VAlert>
                        <VBtn @click="disconnect" :loading="isDisconnecting" :prependIcon="mdiLinkOff" class="mt-3" color="error" variant="tonal">
                            {{ $t("settings.connectors.jira.disconnect") }}
                        </VBtn>
                        <VAlert v-if="error" class="mt-3" type="error" variant="tonal">{{ error }}</VAlert>
                    </template>
                    <template v-else>
                        <VBtn @click="connect" :loading="isConnecting" :prependIcon="mdiJira" color="primary" variant="flat">
                            {{ $t("settings.connectors.jira.connect") }}
                        </VBtn>
                        <VAlert v-if="error" class="mt-3" type="error" variant="tonal">{{ error }}</VAlert>
                    </template>
                </template>
                <span v-else class="text-medium-emphasis">{{ connector.key }}</span>
            </VCardText>
        </VCard>
    </div>
</template>

<script setup lang="ts">
const { t } = useI18n();

const connectorStore = useConnectorStore();
const { connectors, jiraStatus } = storeToRefs(connectorStore);

const isConnecting = ref(false);
const isDisconnecting = ref(false);
const error = ref("");

const errorMessage = (e: unknown): string => {
    const errors = (e as { response?: { data?: { errors?: unknown } } })?.response?.data?.errors;
    if (Array.isArray(errors) && errors.length) return errors.join(" ");
    return t("settings.connectors.jira.error");
};

onBeforeMount(() => {
    connectorStore.executeLoad();
    connectorStore.executeLoadJiraStatus();
});

const iconFor = (key: string): string => (key === "jira" ? mdiJira : mdiConnection);

// Popup OAuth: the settings dialog stays open; the consent runs in a popup that relays the
// result back via postMessage from the static /jira-callback.html relay page.
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

const handleResult = async (data: { code?: string; state?: string; error?: string }, expectedState: string, redirectUri: string) => {
    stopListening();
    activePopup?.close();
    activePopup = null;

    try {
        if (data.error || !data.code || !data.state || data.state !== expectedState) {
            error.value = t("settings.connectors.jira.error");
            return;
        }

        await ConnectorService.connectJira({ code: data.code, state: data.state, redirectUri });
        await connectorStore.executeLoadJiraStatus();
    } catch (e) {
        console.error("jira callback failed", e);
        error.value = errorMessage(e);
    } finally {
        isConnecting.value = false;
    }
};

const disconnect = async () => {
    error.value = "";
    isDisconnecting.value = true;

    try {
        await ConnectorService.disconnectJira();
        await connectorStore.executeLoadJiraStatus();
    } catch (e) {
        console.error("jira disconnect failed", e);
        error.value = errorMessage(e);
    } finally {
        isDisconnecting.value = false;
    }
};

const connect = async () => {
    error.value = "";
    isConnecting.value = true;

    try {
        const redirectUri = `${window.location.origin}/jira-callback.html`;
        const { authorizationUrl, state } = await ConnectorService.authorizeJira(redirectUri);

        activePopup = window.open(authorizationUrl, "jira-oauth", "width=600,height=800");
        if (!activePopup) {
            error.value = t("settings.connectors.jira.popupBlocked");
            isConnecting.value = false;
            return;
        }

        activeListener = (event: MessageEvent) => {
            if (event.origin !== window.location.origin || event.data?.source !== "jira-oauth") return;
            void handleResult(event.data, state, redirectUri);
        };
        window.addEventListener("message", activeListener);

        // Stop the spinner if the user closes the popup without finishing.
        closedTimer = window.setInterval(() => {
            if (activePopup?.closed) {
                stopListening();
                isConnecting.value = false;
            }
        }, 500);
    } catch (e) {
        console.error("jira authorize failed", e);
        error.value = t("settings.connectors.jira.error");
        isConnecting.value = false;
    }
};

onBeforeUnmount(stopListening);
</script>
