import type { ConfigContract } from "@/contracts/ConfigContract";

export const useConfigStore = defineStore("config", () => {
    const { data, execute: executeLoad, isLoading, error } = useAsyncState(ConfigService.get, { initialValue: {} as ConfigContract, shallow: false });

    return {
        data,
        isLoading,
        error,
        executeLoad
    };
});
