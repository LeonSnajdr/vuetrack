<template>
    <Teleport to="#tracking-toolbar-append" defer>
        <VSwitch v-model="groupByDate" label="Group by date" />
        <VBtn id="time-entry-create" @click="create.start" :prependIcon="mdiPlus" color="primary" variant="flat">
            {{ $t("action.create") }}
        </VBtn>
    </Teleport>
    <VDataTable
        v-bind="$attrs"
        :groupBy="groupByDate ? [{ key: 'date' }] : undefined"
        :headers="headers"
        :items="tableItems"
        :itemsPerPage="-1"
        :loading="isLoadingEntry"
        class="overflow-hidden"
        itemValue="id"
        fixedHeader
        hideDefaultFooter
    >
        <template #group-header="{ item, toggleGroup, isGroupOpen }">
            <tr>
                <td colspan="3">
                    <div class="d-flex align-center ga-2">
                        <VBtn @click="toggleGroup(item)" :icon="isGroupOpen(item) ? mdiChevronDown : mdiChevronRight" size="small" variant="text" />
                        <span>{{ item.value }}</span>
                        <span class="text-medium-emphasis">({{ item.items!.length }})</span>
                    </div>
                </td>
                <td>
                    {{ dateHelper.formatDurationMillis(dayDurationByDate[item.value] ?? 0) }}
                </td>
                <td colspan="5" />
            </tr>
        </template>
        <template #item="{ item, props }">
            <VDataTableRow v-bind="props">
                <template #item.data-table-group>
                    {{ item.date }}
                </template>
                <template #item.taskId>
                    <div class="text-truncate" style="max-width: 200px" v-tooltip="item.taskId">{{ item.taskId }}</div>
                </template>
                <template #item.startTime>
                    {{ dateFormatter.format(item.startTime, "fullTime24h") }}
                </template>
                <template #item.endTime>
                    {{ dateFormatter.format(item.endTime, "fullTime24h") }}
                </template>
                <template #item.duration>
                    {{ dateHelper.formatDurationMillis(dateHelper.durationBetween(item.startTime, item.endTime)) }}
                </template>
                <template #item.comment>
                    <div class="text-truncate" style="max-width: 200px" v-tooltip="item.comment">{{ item.comment }}</div>
                </template>
                <template #item.actions>
                    <VIconBtn :id="'time-entry-edit-' + item.id" @click="edit.start(item)" :icon="mdiPencil" variant="text" />
                    <VIconBtn :id="'time-entry-delete-' + item.id" @click="remove.start(item.id)" :icon="mdiDelete" iconColor="error" variant="text" />
                </template>
            </VDataTableRow>
            <tr v-if="item.breakDetails" class="bg-secondary-lighten-2 v-table-break-row">
                <td colspan="3" />
                <td colspan="6">{{ dateHelper.formatDurationMillis(item.breakDetails.durationMillis) }}</td>
            </tr>
        </template>
        <template #loading>
            <VSkeletonLoader type="table-row" />
        </template>
    </VDataTable>
    <TrackingListFeaturesCreateOverlay v-if="interaction.kind === 'create'" v-model:interaction="interaction" />
    <TrackingListFeaturesEditOverlay v-if="interaction.kind === 'edit'" v-model:interaction="interaction" />
    <TrackingListFeaturesDeleteOverlay v-if="interaction.kind === 'delete'" v-model:interaction="interaction" />
</template>

<script setup lang="ts">
import type { DataTableHeader } from "vuetify";
import { useCreate } from "@/components/tracking/list/composables/useCreate";
import { useEdit } from "@/components/tracking/list/composables/useEdit";
import { useDelete } from "@/components/tracking/list/composables/useDelete";
import type { TimeEntryContract } from "@/contracts/TimeEntryContract";

type TimeEntryListContract = TimeEntryContract & { date: string };

const dateFormatter = useDate();
const dateHelper = useDateHelper();
const { t } = useI18n();

const timeEntryStore = useTimeEntryStore();
const listStore = useTrackingListStore();

const { timeEntries } = storeToRefs(timeEntryStore);
const { interaction, isLoadingEntry } = storeToRefs(listStore);

const create = useCreate();
const edit = useEdit();
const remove = useDelete();

const headers: DataTableHeader[] = [
    { title: t("list.table.date"), key: "data-table-group", sortable: false, nowrap: true, width: 200 },
    { title: t("list.table.start"), key: "startTime", sortable: false, nowrap: true, width: 120 },
    { title: t("list.table.end"), key: "endTime", sortable: false, nowrap: true, width: 120 },
    { title: t("list.table.duration"), key: "duration", sortable: false, nowrap: true, width: 120 },
    { title: t("list.table.task"), key: "taskId", sortable: false, nowrap: true, width: 120 },
    { title: t("list.table.project"), key: "project.name", sortable: false, nowrap: true, width: 120 },
    { title: t("list.table.activity"), key: "activity.name", sortable: false, nowrap: true, width: 120 },
    { title: t("list.table.comment"), key: "comment", sortable: false, nowrap: true },
    { title: t("list.table.actions"), key: "actions", sortable: false, align: "end", fixed: "end", width: 100, nowrap: true }
];

const groupByDate = ref(false);

const tableItems = computed((): TimeEntryListContract[] => {
    return timeEntries.value.map((x) => ({
        ...x,
        date: dateFormatter.format(x.startTime, "keyboardDate")
    }));
});

const dayDurationByDate = computed(() => {
    return tableItems.value.reduce<Record<string, number>>((acc, item) => {
        acc[item.date] = (acc[item.date] ?? 0) + dateHelper.durationBetween(item.startTime, item.endTime);
        return acc;
    }, {});
});

useHotkey("#", create.start);

onBeforeUnmount(() => {
    create.cancel();
    edit.cancel();
    remove.cancel();
});
</script>

<style scoped>
:deep(.v-table-break-row) {
    --v-table-row-height: 25px;
}
</style>
