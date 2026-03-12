<template>
    <TimeEntryFeatureEdit v-model="timeEntryUpdate" @edit="finish()" :loading="isUpdating(timeEntry.id)" />
</template>

<script setup lang="ts">
import type { TimeEntryContract, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";

const timeEntry = defineModel<TimeEntryContract>({ required: true });

const timeEntryStore = useTimeEntryStore();

const { isUpdating } = storeToRefs(timeEntryStore);

const timeEntryUpdate = ref<TimeEntryUpdateContract>({
    taskId: timeEntry.value.taskId,
    endTime: timeEntry.value.endTime,
    startTime: timeEntry.value.startTime
});

const finish = () => {
    console.log("finish");
};
</script>
