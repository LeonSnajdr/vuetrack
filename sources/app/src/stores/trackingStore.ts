export const useTrackingStore = defineStore("tracking", () => {
    const preselectedTaskId = ref<string | null>(null);

    return { preselectedTaskId };
});
