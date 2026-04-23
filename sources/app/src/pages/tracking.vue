<template>
    <TrackingSidebar />
    <VContainer class="h-100 d-flex flex-column">
        <VCard class="h-100">
            <TrackingToolbar />
            <RouterView />
        </VCard>
    </VContainer>
</template>

<script setup lang="ts">
const projectStore = useProjectStore();
const timeEntryStore = useTimeEntryStore();
const timeEntrySuggestionStore = useTimeEntrySuggestionStore();

onMounted(() => {
    projectStore.executeLoad();
    timeEntryStore.executeLoadWithFilters();
    timeEntrySuggestionStore.executeLoad();
});
</script>
