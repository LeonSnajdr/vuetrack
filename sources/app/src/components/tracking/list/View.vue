<template>
    <Teleport to="#tracking-toolbar-append" defer>
        <VBtn id="time-entry-create" @click="startCreate" :prependIcon="mdiPlus" color="primary" variant="flat">
            {{ $t("action.create") }}
        </VBtn>
    </Teleport>
    <VDataTable :headers="headers" :items="timeEntries" v-bind="$attrs" :itemsPerPage="-1" class="overflow-hidden" itemValue="id" hideDefaultFooter>
        <template #item="{ item, props }">
            <VDataTableRow v-bind="props">
                <template #item.date>
                    {{ dateFormatter.format(item.startTime, "keyboardDate") }}
                </template>
                <template #item.startTime>
                    {{ dateFormatter.format(item.startTime, "fullTime24h") }}
                </template>
                <template #item.endTime>
                    {{ dateFormatter.format(item.endTime, "fullTime24h") }}
                </template>
                <template #item.duration>
                    {{ formatDuration(item.startTime, item.endTime) }}
                </template>
                <template #item.actions>
                    <VIconBtn :id="'time-entry-edit-' + item.id" @click="timeEntryEdit = item" :icon="mdiPencil" variant="text" />
                    <VIconBtn :id="'time-entry-delete-' + item.id" @click="timeEntryDelete = item" :icon="mdiDelete" iconColor="error" variant="text" />
                </template>
            </VDataTableRow>
            <tr v-if="item.breakDetails" class="bg-secondary-lighten-2 v-table-break-row">
                <td colspan="3" />
                <td colspan="6">{{ formatBreakDuration(item.breakDetails.durationMillis) }}</td>
            </tr>
        </template>
    </VDataTable>
    <TrackingListFeaturesCreateOverlay v-if="timeEntryCreate" @closed="timeEntryCreate = undefined" :timeEntryCreate="timeEntryCreate" />
    <TrackingListFeaturesEditOverlay v-if="timeEntryEdit" @closed="timeEntryEdit = undefined" :timeEntry="timeEntryEdit" />
    <TrackingListFeaturesDeleteOverlay v-if="timeEntryDelete" @closed="timeEntryDelete = undefined" :timeEntry="timeEntryDelete" />
</template>

<script setup lang="ts">
import type { TimeEntryContract, TimeEntryCreateContract } from "@/contracts/TimeEntryContract";
import { VDataTableRow } from "vuetify/components/VDataTable";

const dateFormatter = useDate();
const { t } = useI18n();

const store = useTimeEntryStore();
const trackingStore = useTrackingStore();
const { timeEntries } = storeToRefs(store);
const { startTime, endTime } = storeToRefs(trackingStore);

const timeEntryCreate = ref<TimeEntryCreateContract>();
const timeEntryEdit = ref<TimeEntryContract>();
const timeEntryDelete = ref<TimeEntryContract>();

const headers = computed(() => [
    { title: t("list.table.date"), key: "date", sortable: true },
    { title: t("list.table.start"), key: "startTime", sortable: true },
    { title: t("list.table.end"), key: "endTime", sortable: true },
    { title: t("list.table.duration"), key: "duration", sortable: false },
    { title: t("list.table.task"), key: "taskId", sortable: true },
    { title: t("list.table.project"), key: "project.name", sortable: true },
    { title: t("list.table.activity"), key: "activity.name", sortable: true },
    { title: t("list.table.comment"), key: "comment", sortable: true },
    { title: t("list.table.actions"), key: "actions", sortable: false, align: "end" as const }
]);

const formatDuration = (start: Date, end: Date) => {
    const ms = end.getTime() - start.getTime();
    const hours = Math.floor(ms / (1000 * 60 * 60));
    const minutes = Math.floor((ms % (1000 * 60 * 60)) / (1000 * 60));
    return `${hours}h ${minutes}m`;
};

const formatBreakDuration = (durationMillis: number) => {
    const totalMinutes = Math.max(0, Math.floor(durationMillis / (1000 * 60)));
    const hours = Math.floor(totalMinutes / 60);
    const minutes = totalMinutes % 60;

    if (hours && minutes) {
        return `${hours}h ${minutes}m`;
    }

    if (hours) {
        return `${hours}h`;
    }

    return `${minutes}m`;
};

const createDefaultTimeEntry = (): TimeEntryCreateContract => {
    const hourMs = 60 * 60 * 1000;
    const rangeStart = startTime.value.getTime();
    const rangeEnd = endTime.value.getTime();
    const defaultStartMs = Math.max(rangeStart, Math.min(Date.now(), rangeEnd - hourMs));

    return {
        taskId: "",
        startTime: new Date(defaultStartMs),
        endTime: new Date(defaultStartMs + hourMs),
        activityId: null,
        projectId: null,
        comment: ""
    };
};

const startCreate = () => {
    timeEntryCreate.value = createDefaultTimeEntry();
};

useHotkey("#", startCreate);
</script>

<style>
.v-table-break-row {
    --v-table-row-height: 25px;
}
</style>
