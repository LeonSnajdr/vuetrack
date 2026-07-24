import type { ConnectorDescriptorContract, ConnectorStatusResponse } from "@/contracts/ConnectorContract";
import { ConnectorCapability } from "@/contracts/ConnectorContract";

export const useConnectorStore = defineStore("connector", () => {
    const notify = useNotify();
    const { t } = useI18n();
    const popup = useOAuthPopup();

    const { data: connectors, execute: executeLoad, isLoading } = useAsyncState(ConnectorService.list, { initialValue: [], shallow: false });

    const connectingKeys = ref<Record<string, boolean>>({});
    const disconnectingKeys = ref<Record<string, boolean>>({});

    const statusFor = (key: string): ConnectorStatusResponse => {
        const connector = connectors.value.find((c) => c.key === key);
        return { connected: connector?.connected ?? false, healthy: connector?.healthy ?? false };
    };

    const isConnecting = (key: string): boolean => connectingKeys.value[key] ?? false;

    const isDisconnecting = (key: string): boolean => disconnectingKeys.value[key] ?? false;

    const isOAuth = (connector: ConnectorDescriptorContract): boolean => connector.capabilities.includes(ConnectorCapability.OAuth);

    const nameFor = (key: string): string => t(`settings.connectors.name.${key}`);

    const connect = async (key: string) => {
        connectingKeys.value[key] = true;

        try {
            const redirectUri = `${window.location.origin}/${key}-callback.html`;
            const { authorizationUrl, state } = await ConnectorService.authorize(key, redirectUri);

            const result = await popup.open(key, authorizationUrl, state);

            if (result.status === "cancelled") {
                return;
            }
            if (result.status === "blocked") {
                notify.error(t("settings.connectors.popupBlocked"));
                return;
            }
            if (result.status === "error") {
                notify.error(t("settings.connectors.error", { name: nameFor(key) }));
                return;
            }

            await ConnectorService.connect(key, { code: result.code, state: result.state, redirectUri });
            await executeLoad();
        } catch (e) {
            console.error(`${key} connect failed`, e);
            notify.error(t("settings.connectors.error", { name: nameFor(key) }), { error: e });
        } finally {
            connectingKeys.value[key] = false;
        }
    };

    const disconnect = async (key: string) => {
        disconnectingKeys.value[key] = true;

        try {
            await ConnectorService.disconnect(key);
            await executeLoad();
        } catch (e) {
            console.error(`${key} disconnect failed`, e);
            notify.error(t("settings.connectors.error", { name: nameFor(key) }), { error: e });
        } finally {
            disconnectingKeys.value[key] = false;
        }
    };

    return {
        connectors,
        isLoading,
        executeLoad,
        statusFor,
        isConnecting,
        isDisconnecting,
        isOAuth,
        nameFor,
        connect,
        disconnect,
        cancelPending: popup.cancel
    };
});
