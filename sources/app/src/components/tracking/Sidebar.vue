<template>
    <VNavigationDrawer v-if="sidebarOpen" location="left" width="280" permanent>
        <TrackingPresetList />
    </VNavigationDrawer>
</template>

<script setup lang="ts">
const trackingStore = useTrackingStore();
const { sidebarOpen } = storeToRefs(trackingStore);
</script>
