<template>
    <div class="h-100 d-flex flex-column ga-4">
        <VCard v-for="backend in backends" :key="backend.key" class="border" variant="outlined">
            <VCardTitle>
                <VIcon :icon="iconMap[backend.key]" />
                {{ nameFor(backend.key) }}
                <VSpacer />
                <VChip :color="chipFor(backend.key).color" variant="tonal">
                    <template #prepend>
                        <VIcon :color="chipFor(backend.key).color" :icon="chipFor(backend.key).icon" class="mr-2" />
                    </template>
                    {{ chipFor(backend.key).text }}
                </VChip>
            </VCardTitle>
            <VCardText>
                <template v-if="isOAuth(backend)">
                    <div v-if="statusFor(backend.key).connected" class="d-flex ga-2">
                        <VBtn
                            v-if="!statusFor(backend.key).healthy"
                            @click="connect(backend.key)"
                            :loading="isConnecting(backend.key)"
                            :prependIcon="iconMap[backend.key]"
                            color="primary"
                            variant="flat"
                        >
                            {{ $t("settings.backends.reconnect", { name: nameFor(backend.key) }) }}
                        </VBtn>
                        <VBtn
                            @click="disconnect(backend.key)"
                            :loading="isDisconnecting(backend.key)"
                            :prependIcon="mdiLinkOff"
                            color="error"
                            variant="tonal"
                        >
                            {{ $t("settings.backends.disconnect") }}
                        </VBtn>
                    </div>
                    <VBtn
                        v-else
                        @click="connect(backend.key)"
                        :loading="isConnecting(backend.key)"
                        :prependIcon="iconMap[backend.key]"
                        color="primary"
                        variant="flat"
                    >
                        {{ $t("settings.backends.connect", { name: nameFor(backend.key) }) }}
                    </VBtn>
                </template>
                <span v-else class="text-medium-emphasis">{{ nameFor(backend.key) }}</span>
            </VCardText>
        </VCard>
    </div>
</template>

<script setup lang="ts">
const { t } = useI18n();

const backendStore = useBackendStore();
const { backends } = storeToRefs(backendStore);
const { statusFor, isConnecting, isDisconnecting, isOAuth, nameFor, connect, disconnect } = backendStore;

const iconMap: Record<string, string> = { timetracking: mdiClockOutline };

const chipFor = (key: string) => {
    const status = statusFor(key);
    if (status.connected && status.healthy) {
        return { color: "success", icon: mdiCircle, text: t("settings.backends.connected") };
    }
    if (status.connected) {
        return { color: "warning", icon: mdiAlert, text: t("settings.backends.reconnectRequired") };
    }
    return { color: "", icon: mdiCircleOutline, text: t("settings.backends.notConnected") };
};

onBeforeUnmount(backendStore.cancelPending);
</script>
