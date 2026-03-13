<template>
    <VToolbar class="px-2" flat>
        <TrackingTimePeriodSelection v-model:startTime="startTime" v-model:endTime="endTime" />
        <template #append>
            <div class="d-flex align-center ga-2">
                <div id="tracking-toolbar-actions" class="d-inline-flex align-center ga-2" />
                <VBtnToggle density="compact" variant="outlined">
                    <VBtn :to="{ name: '/tracking/calendar' }"> Calendar </VBtn>
                    <VBtn :to="{ name: '/tracking/list' }"> List</VBtn>
                </VBtnToggle>
            </div>
        </template>
    </VToolbar>
</template>

<script setup lang="ts">
const trackingStore = useTrackingStore();
const { startTime, endTime } = storeToRefs(trackingStore);
</script>
