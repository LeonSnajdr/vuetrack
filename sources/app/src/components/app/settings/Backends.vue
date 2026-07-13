<template>
    <div class="h-100 d-flex flex-column ga-4">
        <p class="text-medium-emphasis">{{ $t("settings.backends.description") }}</p>

        <VCard class="border" variant="outlined">
            <VCardTitle>
                <VIcon :icon="mdiClockOutline" />
                Timetracking
            </VCardTitle>
            <VCardText>
                <template v-if="timetrackingStatus.connected">
                    <VAlert :icon="mdiCheckCircle" type="success" variant="tonal">
                        {{ $t("settings.backends.timetracking.connected") }}
                    </VAlert>
                    <VBtn @click="disconnect" :loading="isDisconnecting" :prependIcon="mdiLinkOff" class="mt-3" color="error" variant="tonal">
                        {{ $t("settings.backends.timetracking.disconnect") }}
                    </VBtn>
                    <VAlert v-if="error" class="mt-3" type="error" variant="tonal">{{ error }}</VAlert>
                </template>
                <template v-else>
                    <VBtn @click="connect" :loading="isConnecting" :prependIcon="mdiClockOutline" color="primary" variant="flat">
                        {{ $t("settings.backends.timetracking.connect") }}
                    </VBtn>
                    <VAlert v-if="error" class="mt-3" type="error" variant="tonal">{{ error }}</VAlert>
                </template>
            </VCardText>
        </VCard>
    </div>
</template>

<script setup lang="ts">
const { t } = useI18n();

const backendStore = useBackendStore();
const { timetrackingStatus } = storeToRefs(backendStore);

const isConnecting = ref(false);
const isDisconnecting = ref(false);
const error = ref("");

const errorMessage = (e: unknown): string => {
    const errors = (e as { response?: { data?: { errors?: unknown } } })?.response?.data?.errors;
    if (Array.isArray(errors) && errors.length) return errors.join(" ");
    return t("settings.backends.timetracking.error");
};

onBeforeMount(() => {
    backendStore.executeLoad();
    backendStore.executeLoadTimetrackingStatus();
});

// Popup OAuth: the settings dialog stays open; the consent runs in a popup that relays the
// result back via postMessage from the static /timetracking-callback.html relay page.
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
            error.value = t("settings.backends.timetracking.error");
            return;
        }

        await BackendService.connectTimetracking({ code: data.code, state: data.state, redirectUri });
        await backendStore.executeLoadTimetrackingStatus();
    } catch (e) {
        console.error("timetracking callback failed", e);
        error.value = errorMessage(e);
    } finally {
        isConnecting.value = false;
    }
};

const disconnect = async () => {
    error.value = "";
    isDisconnecting.value = true;

    try {
        await BackendService.disconnectTimetracking();
        await backendStore.executeLoadTimetrackingStatus();
    } catch (e) {
        console.error("timetracking disconnect failed", e);
        error.value = errorMessage(e);
    } finally {
        isDisconnecting.value = false;
    }
};

const connect = async () => {
    error.value = "";
    isConnecting.value = true;

    try {
        const redirectUri = `${window.location.origin}/timetracking-callback.html`;
        const { authorizationUrl, state } = await BackendService.authorizeTimetracking(redirectUri);

        activePopup = window.open(authorizationUrl, "timetracking-oauth", "width=600,height=800");
        if (!activePopup) {
            error.value = t("settings.backends.timetracking.popupBlocked");
            isConnecting.value = false;
            return;
        }

        activeListener = (event: MessageEvent) => {
            if (event.origin !== window.location.origin || event.data?.source !== "timetracking-oauth") return;
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
        console.error("timetracking authorize failed", e);
        error.value = t("settings.backends.timetracking.error");
        isConnecting.value = false;
    }
};

onBeforeUnmount(stopListening);
</script>
