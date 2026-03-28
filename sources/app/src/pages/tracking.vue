<template>
    <TrackingCalendarSidebar />
    <RouterView />
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
