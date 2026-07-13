import type { TimetrackingStatusResponse } from "@/contracts/BackendContract";

export const useBackendStore = defineStore("backend", () => {
    const { data: backends, execute: executeLoad, isLoading } = useAsyncState(BackendService.list, { initialValue: [], shallow: false });

    const initialStatus: TimetrackingStatusResponse = { connected: false };
    const {
        data: timetrackingStatus,
        execute: executeLoadTimetrackingStatus,
        isLoading: isLoadingTimetrackingStatus
    } = useAsyncState(BackendService.timetrackingStatus, {
        initialValue: initialStatus,
        shallow: false
    });

    return {
        backends,
        timetrackingStatus,
        isLoading,
        isLoadingTimetrackingStatus,
        executeLoad,
        executeLoadTimetrackingStatus
    };
});
