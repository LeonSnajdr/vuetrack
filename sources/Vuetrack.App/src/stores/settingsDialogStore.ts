export const useSettingsDialogStore = defineStore("settingsDialog", () => {
    const isOpen = ref(false);
    const activeTab = ref("general");

    const open = (tab: string = "general") => {
        activeTab.value = tab;
        isOpen.value = true;
    };

    return {
        isOpen,
        activeTab,
        open
    };
});
