export const useProjectStore = defineStore("project", () => {
    const { data: projects, execute: executeLoad, isLoading } = useAsyncState(ProjectService.load, { initialValue: [], shallow: false });

    return {
        projects,
        executeLoad,
        isLoading
    };
});
