<template>
    <TrackingCalendarSidebar />
    <VBtn @click="add">Add</VBtn>
    <VBtn @click="removeLast" :disabled="timeEntries.length === 0" class="ml-2" color="error" variant="outlined"> Remove last </VBtn>
    <VBtn @click="addOneHour">Add 1 hour</VBtn>
    <template v-if="timeEntries[0]">
        <p>{{ timeEntries[0].startTime }} /{{ timeEntries[0].endTime }}</p>
        <VTextField v-model="timeEntries[0].taskId" />
    </template>
    <VContainer class="h-100">
        <TrackingCalendarView v-model:timeEntries="timeEntries" v-model:timeEntrySuggestions="timeEntrySuggestions" />
    </VContainer>
</template>

<script setup lang="ts">
import type { TimeEntrySuggestionContract, TimeEntrySuggestionId } from "@/contracts/TimeEntrySuggestion";

const timeEntries = ref<TimeEntryContract[]>([
    {
        id: "myId" as TimeEntryId,
        user: "leon",
        startTime: new Date(),
        endTime: new Date(Date.now() + 60 * 60 * 1000),
        taskId: "myTask"
    }
]);

const timeEntrySuggestions = ref<TimeEntrySuggestionContract[]>([
    {
        id: "mySuggestion" as TimeEntrySuggestionId,
        startTime: new Date((Date.now() - 1 * 24 * 60 * 60 * 1000) + 60 * 60 * 1000),
        endTime: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000),
        taskId: "my suggested id"
    }
]);

const add = () => {
    timeEntries.value.push({
        id: "myId" as TimeEntryId,
        user: "leon",
        startTime: new Date(),
        endTime: new Date(Date.now() + 60 * 60 * 1000),
        taskId: "myTask"
    });
};

const removeLast = () => {
    timeEntries.value.pop();
};

const addOneHour = () => {
    const entry = timeEntries.value[0];
    if (!entry) return;

    entry.endTime = new Date(entry.endTime.getTime() + 60 * 60 * 1000);
};
</script>
