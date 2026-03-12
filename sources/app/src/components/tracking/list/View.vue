<template>
    <div class="d-flex justify-end mb-4">
        <VBtn id="time-entry-create" @click="startCreate" :prependIcon="mdiPlus" color="primary" variant="flat">
            {{ $t("action.add") }}
        </VBtn>
    </div>

    <VDataTable :headers="headers" :items="timeEntries" itemValue="id" hover>
        <template #item.startTime="{ value }">
            {{ dateFormatter.format(value, "fullDateTime24h") }}
        </template>
        <template #item.endTime="{ value }">
            {{ dateFormatter.format(value, "fullDateTime24h") }}
        </template>
        <template #item.duration="{ item }">
            {{ formatDuration(item.startTime, item.endTime) }}
        </template>
        <template #item.actions="{ item }">
            <VIconBtn :id="'time-entry-edit-' + item.id" @click="timeEntryEdit = item" :icon="mdiPencil" variant="text" />
            <VIconBtn :id="'time-entry-delete-' + item.id" @click="timeEntryDelete = item" :icon="mdiDelete" iconColor="error" variant="text" />
        </template>
    </VDataTable>

    <TrackingListFeaturesCreateOverlay
        v-if="timeEntryCreate"
        @closed="timeEntryCreate = undefined"
        :timeEntryCreate="timeEntryCreate"
    />

    <TrackingListFeaturesEditOverlay
        v-if="timeEntryEdit"
        @closed="timeEntryEdit = undefined"
        :timeEntry="timeEntryEdit"
    />

    <TrackingListFeaturesDeleteOverlay
        v-if="timeEntryDelete"
        @closed="timeEntryDelete = undefined"
        :timeEntry="timeEntryDelete"
    />
</template>

<script setup lang="ts">
import type { TimeEntryContract, TimeEntryCreateContract } from "@/contracts/TimeEntryContract";

const dateFormatter = useDate();

const store = useTimeEntryStore();
const trackingStore = useTrackingStore();
const { timeEntries } = storeToRefs(store);
const { startTime, endTime } = storeToRefs(trackingStore);

const timeEntryCreate = ref<TimeEntryCreateContract>();
const timeEntryEdit = ref<TimeEntryContract>();
const timeEntryDelete = ref<TimeEntryContract>();

const headers = [
    { title: "Task", key: "taskId", sortable: true },
    { title: "Start", key: "startTime", sortable: true },
    { title: "End", key: "endTime", sortable: true },
    { title: "Duration", key: "duration", sortable: false },
    { title: "Actions", key: "actions", sortable: false, align: "end" as const }
];

const formatDuration = (start: Date, end: Date) => {
    const ms = end.getTime() - start.getTime();
    const hours = Math.floor(ms / (1000 * 60 * 60));
    const minutes = Math.floor((ms % (1000 * 60 * 60)) / (1000 * 60));
    return `${hours}h ${minutes}m`;
};

const createDefaultTimeEntry = (): TimeEntryCreateContract => {
    const hourMs = 60 * 60 * 1000;
    const rangeStart = startTime.value.getTime();
    const rangeEnd = endTime.value.getTime();
    const defaultStartMs = Math.max(rangeStart, Math.min(Date.now(), rangeEnd - hourMs));

    return {
        taskId: "",
        startTime: new Date(defaultStartMs),
        endTime: new Date(defaultStartMs + hourMs)
    };
};

const startCreate = () => {
    timeEntryCreate.value = createDefaultTimeEntry();
};
</script>
