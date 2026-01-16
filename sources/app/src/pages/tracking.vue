<template>
    <TrackingCalendarSidebar />
    <VBtn @click="timeEntryStore.add()">Add</VBtn>
    <VBtn @click="timeEntryStore.removeLast()" :disabled="timeEntries.length === 0" class="ml-2" color="error" variant="outlined"> Remove last </VBtn>
    <VBtn @click="timeEntryStore.addOneHour()">Add 1 hour</VBtn>
    <template v-if="timeEntries[0]">
        <p>{{ timeEntries[0].startTime }} /{{ timeEntries[0].endTime }}</p>
        <VTextField v-model="timeEntries[0].taskId" />
    </template>
    <VContainer class="h-100">
        <TrackingCalendarView />
    </VContainer>
</template>

<script setup lang="ts">
const timeEntryStore = useTimeEntryStore();

const { timeEntries } = storeToRefs(timeEntryStore);
</script>
