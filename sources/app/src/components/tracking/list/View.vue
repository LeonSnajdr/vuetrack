<template>
    <Teleport to="#tracking-toolbar-append" defer>
        <VBtn id="time-entry-create" @click="timeEntryCreate = true" :prependIcon="mdiPlus" color="primary" variant="flat">
            {{ $t("action.create") }}
        </VBtn>
    </Teleport>
    <VDataTable :headers="headers" :items="timeEntries" v-bind="$attrs" :itemsPerPage="-1" class="overflow-hidden" itemValue="id" fixedHeader hideDefaultFooter>
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
                <template #item.comment>
                    <div class="text-truncate" style="max-width: 200px" v-tooltip="item.comment">{{ item.comment }}</div>
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
    <TrackingListFeaturesCreateOverlay v-if="timeEntryCreate" @closed="timeEntryCreate = false" />
    <TrackingListFeaturesEditOverlay v-if="timeEntryEdit" @closed="timeEntryEdit = undefined" :timeEntry="timeEntryEdit" />
    <TrackingListFeaturesDeleteOverlay v-if="timeEntryDelete" @closed="timeEntryDelete = undefined" :timeEntry="timeEntryDelete" />
</template>

<script setup lang="ts">
import type { TimeEntryContract } from "@/contracts/TimeEntryContract";
import type { DataTableHeader } from "vuetify";

const dateFormatter = useDate();
const { t } = useI18n();

const store = useTimeEntryStore();
const { timeEntries } = storeToRefs(store);

const timeEntryCreate = ref(false);
const timeEntryEdit = ref<TimeEntryContract>();
const timeEntryDelete = ref<TimeEntryContract>();

const headers: DataTableHeader[] = [
    { title: t("list.table.date"), key: "date", sortable: true, nowrap: true },
    { title: t("list.table.start"), key: "startTime", sortable: true, nowrap: true },
    { title: t("list.table.end"), key: "endTime", sortable: true, nowrap: true },
    { title: t("list.table.duration"), key: "duration", sortable: false, nowrap: true },
    { title: t("list.table.task"), key: "taskId", sortable: true, nowrap: true },
    { title: t("list.table.project"), key: "project.name", sortable: true, nowrap: true },
    { title: t("list.table.activity"), key: "activity.name", sortable: true, nowrap: true },
    { title: t("list.table.comment"), key: "comment", sortable: true, nowrap: true },
    { title: t("list.table.actions"), key: "actions", sortable: false, align: "end", fixed: "end", width: 100, nowrap: true }
];

whenever(timeEntryCreate, () => {
    timeEntryEdit.value = undefined;
    timeEntryDelete.value = undefined;
});

whenever(
    () => timeEntryEdit.value !== undefined,
    () => {
        timeEntryCreate.value = false;
        timeEntryDelete.value = undefined;
    }
);

whenever(
    () => timeEntryDelete.value !== undefined,
    () => {
        timeEntryCreate.value = false;
        timeEntryEdit.value = undefined;
    }
);

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

useHotkey("#", () => (timeEntryCreate.value = true));
</script>

<style scoped>
:deep(.v-table-break-row) {
    --v-table-row-height: 25px;
}
</style>
