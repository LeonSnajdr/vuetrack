<template>
    <TrackingCalendarFeaturesCreateOverlay v-if="interaction.kind === 'create'" :key="interaction.event.uiId" v-model:interaction="interaction" />
    <TrackingCalendarFeaturesEditOverlay v-if="interaction.kind === 'edit'" :key="interaction.event.uiId" v-model:interaction="interaction" />
    <TrackingCalendarFeaturesConflictOverlay v-if="interaction.kind === 'conflict'" :key="interaction.event.uiId" v-model:interaction="interaction" />
    <TrackingCalendarFeaturesDeleteOverlay v-if="interaction.kind === 'delete'" :key="interaction.event.uiId" v-model:interaction="interaction" />
</template>

<script setup lang="ts">
const calendarStore = useCalendarStore();
const { interaction } = storeToRefs(calendarStore);
</script>
