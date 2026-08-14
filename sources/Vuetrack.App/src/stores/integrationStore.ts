import type { IntegrationStatusResponse } from "@/contracts/IntegrationContract";
import { IntegrationKey } from "@/contracts/IntegrationContract";

export const useIntegrationStore = defineStore("integration", () => {
    const notify = useNotify();
    const { t } = useI18n();
    const popup = useOAuthPopup();

    const { data: integrations, execute: executeLoad, isLoading } = useAsyncState(IntegrationService.list, { initialValue: [], shallow: false });

    const connectingKeys = ref<Record<string, boolean>>({});
    const disconnectingKeys = ref<Record<string, boolean>>({});

    const hasUsableBackend = computed(() => integrations.value.some((i) => i.key === IntegrationKey.Timetracking && i.connected && i.healthy));

    const listFor = (keys: IntegrationKey[]) => computed(() => integrations.value.filter((i) => keys.includes(i.key)));

    const statusFor = (key: IntegrationKey): IntegrationStatusResponse => {
        const integration = integrations.value.find((i) => i.key === key);
        return { connected: integration?.connected ?? false, healthy: integration?.healthy ?? false };
    };

    const isConnecting = (key: IntegrationKey): boolean => connectingKeys.value[key] ?? false;

    const isDisconnecting = (key: IntegrationKey): boolean => disconnectingKeys.value[key] ?? false;

    const nameFor = (key: IntegrationKey): string => t(`settings.integration.name.${key}`);

    const connect = async (key: IntegrationKey) => {
        connectingKeys.value[key] = true;

        try {
            const redirectUri = `${window.location.origin}/${key}-callback.html`;
            const { authorizationUrl, state } = await IntegrationService.authorize(key, redirectUri);

            const result = await popup.open(key, authorizationUrl, state);

            if (result.status === "cancelled") {
                return;
            }
            if (result.status === "blocked") {
                notify.error(t("settings.integration.popupBlocked"));
                return;
            }
            if (result.status === "error") {
                notify.error(t("settings.integration.error", { name: nameFor(key) }));
                return;
            }

            await IntegrationService.connect(key, { code: result.code, state: result.state, redirectUri });
            await executeLoad();
        } catch (e) {
            console.error(`${key} connect failed`, e);
            notify.error(t("settings.integration.error", { name: nameFor(key) }), { error: e });
        } finally {
            connectingKeys.value[key] = false;
        }
    };

    const disconnect = async (key: IntegrationKey) => {
        disconnectingKeys.value[key] = true;

        try {
            await IntegrationService.disconnect(key);
            await executeLoad();
        } catch (e) {
            console.error(`${key} disconnect failed`, e);
            notify.error(t("settings.integration.error", { name: nameFor(key) }), { error: e });
        } finally {
            disconnectingKeys.value[key] = false;
        }
    };

    return {
        integrations,
        isLoading,
        hasUsableBackend,
        executeLoad,
        listFor,
        statusFor,
        isConnecting,
        isDisconnecting,
        nameFor,
        connect,
        disconnect,
        cancelPending: popup.cancel
    };
});
