import type { JiraStatusResponse } from "@/contracts/ConnectorContract";

export const useConnectorStore = defineStore("connector", () => {
    const { data: connectors, execute: executeLoad, isLoading } = useAsyncState(ConnectorService.list, { initialValue: [], shallow: false });

    const initialJiraStatus: JiraStatusResponse = { connected: false, siteUrl: null };
    const { data: jiraStatus, execute: executeLoadJiraStatus, isLoading: isLoadingJiraStatus } = useAsyncState(ConnectorService.jiraStatus, {
        initialValue: initialJiraStatus,
        shallow: false
    });

    return {
        connectors,
        jiraStatus,
        isLoading,
        isLoadingJiraStatus,
        executeLoad,
        executeLoadJiraStatus
    };
});
