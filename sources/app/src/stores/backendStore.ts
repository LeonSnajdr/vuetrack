import type { BackendDescriptorContract, BackendStatusResponse } from "@/contracts/BackendContract";
import { BackendCapability } from "@/contracts/BackendContract";

export const useBackendStore = defineStore("backend", () => {
    const notify = useNotify();
    const { t } = useI18n();
    const popup = useOAuthPopup();

    const { data: backends, execute: executeLoad, isLoading } = useAsyncState(BackendService.list, { initialValue: [], shallow: false });

    const connectingKeys = ref<Record<string, boolean>>({});
    const disconnectingKeys = ref<Record<string, boolean>>({});

    const statusFor = (key: string): BackendStatusResponse => {
        const backend = backends.value.find((b) => b.key === key);
        return { connected: backend?.connected ?? false, healthy: backend?.healthy ?? false };
    };

    const isConnecting = (key: string): boolean => connectingKeys.value[key] ?? false;

    const isDisconnecting = (key: string): boolean => disconnectingKeys.value[key] ?? false;

    const isOAuth = (backend: BackendDescriptorContract): boolean => backend.capabilities.includes(BackendCapability.OAuth);

    const nameFor = (key: string): string => t(`settings.backends.name.${key}`);

    const connect = async (key: string) => {
        connectingKeys.value[key] = true;

        try {
            const redirectUri = `${window.location.origin}/${key}-callback.html`;
            const { authorizationUrl, state } = await BackendService.authorize(key, redirectUri);

            const result = await popup.open(key, authorizationUrl, state);

            if (result.status === "cancelled") {
                return;
            }
            if (result.status === "blocked") {
                notify.error(t("settings.backends.popupBlocked"));
                return;
            }
            if (result.status === "error") {
                notify.error(t("settings.backends.error", { name: nameFor(key) }));
                return;
            }

            await BackendService.connect(key, { code: result.code, state: result.state, redirectUri });
            await executeLoad();
        } catch (e) {
            console.error(`${key} connect failed`, e);
            notify.error(t("settings.backends.error", { name: nameFor(key) }), { error: e });
        } finally {
            connectingKeys.value[key] = false;
        }
    };

    const disconnect = async (key: string) => {
        disconnectingKeys.value[key] = true;

        try {
            await BackendService.disconnect(key);
            await executeLoad();
        } catch (e) {
            console.error(`${key} disconnect failed`, e);
            notify.error(t("settings.backends.error", { name: nameFor(key) }), { error: e });
        } finally {
            disconnectingKeys.value[key] = false;
        }
    };

    return {
        backends,
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
