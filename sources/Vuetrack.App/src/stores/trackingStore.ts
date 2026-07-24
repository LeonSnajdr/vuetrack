export const useTrackingStore = defineStore(
    "tracking",
    () => {
        const sidebarOpen = ref(true);

        return { sidebarOpen };
    },
    {
        persist: true
    }
);
