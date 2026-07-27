<template>
    <TrackingCalendarFeaturesCreateOverlay v-if="task.kind === 'create'" :key="task.event.uiId" v-model:task="task" />
    <TrackingCalendarFeaturesEditOverlay v-if="task.kind === 'edit'" :key="task.event.uiId" v-model:task="task" />
    <TrackingCalendarFeaturesConflictOverlay v-if="task.kind === 'conflict'" :key="task.event.uiId" v-model:task="task" />
    <TrackingCalendarFeaturesDeleteOverlay v-if="task.kind === 'delete'" :key="task.event.uiId" v-model:task="task" />
</template>

<script setup lang="ts">
const calendarStore = useCalendarStore();
const { task } = storeToRefs(calendarStore);
</script>
