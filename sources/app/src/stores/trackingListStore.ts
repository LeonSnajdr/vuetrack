import type { Interaction } from "@/components/tracking/list/types";

export const useTrackingListStore = defineStore("trackingList", () => {
    const timeEntryStore = useTimeEntryStore();

    const interaction = ref<Interaction>({ kind: "idle" });

    const isLoadingEntry = computed(() => timeEntryStore.isLoading);
    const isCreatingEntry = computed(() => timeEntryStore.isCreating());
    const isUpdatingEntry = computed(() => timeEntryStore.isUpdating());
    const isDeletingEntry = computed(() => timeEntryStore.isDeleting());

    return {
        interaction,
        isLoadingEntry,
        isCreatingEntry,
        isUpdatingEntry,
        isDeletingEntry
    };
});
