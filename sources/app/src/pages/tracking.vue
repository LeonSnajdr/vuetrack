<template>
    <TrackingCalendarSidebar />
    <VBtn @click="timeEntryStore.add()">{{ $t("action.add") }}</VBtn>
    <VBtn @click="timeEntryStore.removeLast()" :disabled="timeEntries.length === 0" class="ml-2" color="error" variant="outlined">{{
        $t("tracking.removeLast")
    }}</VBtn>
    <VBtn @click="timeEntryStore.addOneHour()">{{ $t("tracking.addOneHour") }}</VBtn>
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
