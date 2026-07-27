<template>
    <AppHeader />
    <TrackingSidebar />
    <VContainer class="h-100 d-flex flex-column">
        <VCard :loading="isLoading" class="h-100">
            <template v-if="!isLoading">
                <TrackingEmptyState v-if="!hasUsableBackend" />
                <template v-else>
                    <TrackingToolbar />
                    <RouterView />
                </template>
            </template>
        </VCard>
    </VContainer>
</template>

<script setup lang="ts">
definePage({ redirect: "/tracking/calendar" });

const projectStore = useProjectStore();
const timeEntryStore = useTimeEntryStore();
const timeEntrySuggestionStore = useTimeEntrySuggestionStore();

const backendStore = useBackendStore();
const { hasUsableBackend, isLoading } = storeToRefs(backendStore);

whenever(
    hasUsableBackend,
    () => {
        projectStore.executeLoad();
        timeEntryStore.executeLoadWithFilters();
        timeEntrySuggestionStore.executeLoadWithFilters();
    },
    { immediate: true }
);
</script>
