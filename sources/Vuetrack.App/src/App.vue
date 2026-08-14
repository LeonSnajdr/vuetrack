<template>
    <VApp>
        <VMain class="h-screen">
            <template v-if="isInitialized">
                <AppNotificationView />
                <RouterView />
            </template>

            <VEmptyState v-else-if="loading" :text="$t('app.initialize.loading')">
                <template #media>
                    <BaseMascotLoader />
                </template>
            </VEmptyState>

            <VEmptyState
                v-else
                @click:action="initializeApp"
                :actionText="$t('app.initialize.error.actionText')"
                :icon="mdiAlertCircleOutline"
                :text="$t('app.initialize.error.text')"
                :title="$t('app.initialize.error.headline')"
                color="primary"
            />
        </VMain>
    </VApp>
</template>

<script lang="ts" setup>
import { useTheme } from "vuetify";
import { setupAuth } from "@/plugins/auth";

const settingsStore = useSettingsStore();
const { generalSettings } = storeToRefs(settingsStore);

const configStore = useConfigStore();
const integrationStore = useIntegrationStore();

const theme = useTheme();

const loading = ref(true);
const isInitialized = ref(false);

const initializeApp = async () => {
    loading.value = true;
    isInitialized.value = false;

    try {
        await configStore.executeLoad();

        if (configStore.error) {
            return;
        }

        await setupAuth(configStore.data.authOptions);
        isInitialized.value = true;
    } catch (e) {
        console.error("error initializing app", e);
    } finally {
        loading.value = false;
    }
};

onBeforeMount(initializeApp);

watch(isInitialized, (value) => {
    if (!value) return;

    integrationStore.executeLoad();
});

watch(
    () => generalSettings.value.theme,
    (value) => theme.change(value),
    { immediate: true }
);
</script>

<style lang="scss">
@use "@/styles/settings";
@use "@/styles/global";

$header-height: 48px;
$container-padding: settings.$container-padding-x;

.v-main {
    &:has(.v-navigation-drawer--left) {
        margin-left: $container-padding;
        width: calc(100% - #{$container-padding}) !important;

        .v-navigation-drawer--left {
            margin-left: $container-padding;
            margin-top: $container-padding;
            height: calc(100% - #{$header-height + $container-padding * 2}) !important;
        }
    }

    &:has(.v-navigation-drawer--right) {
        margin-right: $container-padding;
        width: calc(100% - #{$container-padding}) !important;

        .v-navigation-drawer--right {
            margin-right: $container-padding;
            margin-top: $container-padding;
            height: calc(100% - #{$header-height + $container-padding * 2}) !important;
        }
    }

    &:has(.v-navigation-drawer--left):has(.v-navigation-drawer--right) {
        width: calc(100% - #{$container-padding * 2}) !important;
    }

    .v-app-bar {
        margin-left: $container-padding;
        width: calc(100% - #{$container-padding * 2}) !important;
    }
}
</style>
