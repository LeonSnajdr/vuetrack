import type { ConnectorStatusResponse } from "@/contracts/ConnectorContract";

export const useConnectorStore = defineStore("connector", () => {
    const { data: connectors, execute: executeLoad, isLoading } = useAsyncState(ConnectorService.list, { initialValue: [], shallow: false });

    const statuses = ref<Record<string, ConnectorStatusResponse>>({});

    const loadStatus = async (key: string) => {
        statuses.value[key] = await ConnectorService.status(key);
    };

    return {
        connectors,
        statuses,
        isLoading,
        executeLoad,
        loadStatus
    };
});
