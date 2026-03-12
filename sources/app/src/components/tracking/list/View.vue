<template>
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
            <VIconBtn @click="remove(item.id)" :icon="mdiDelete" iconColor="error" variant="text" />
        </template>
    </VDataTable>

    <TrackingListFeaturesEditOverlay
        v-if="timeEntryEdit"
        @closed="timeEntryEdit = undefined"
        :target="'#time-entry-edit-' + timeEntryEdit.id"
        :timeEntry="timeEntryEdit"
    />
</template>

<script setup lang="ts">
import type { TimeEntryContract, TimeEntryId } from "@/contracts/TimeEntryContract";

const store = useTimeEntryStore();
const { timeEntries } = storeToRefs(store);
const dateFormatter = useDate();
const notify = useNotify();

const timeEntryEdit = ref<TimeEntryContract>();

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

const remove = async (id: TimeEntryId) => {
    const result = await store.remove(id);
    if (result.status === "success") {
        notify.success($t("action.delete"));
    } else {
        notify.error($t("action.delete.error"));
    }
};

const { t: $t } = useI18n();
</script>
