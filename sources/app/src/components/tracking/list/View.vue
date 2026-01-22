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
            <VIconBtn @click="startEdit(item)" :icon="mdiPencil" variant="text" />
            <VIconBtn @click="remove(item.id)" :icon="mdiDelete" iconColor="error" variant="text" />
        </template>
    </VDataTable>

    <VDialog v-model="editDialog" maxWidth="400" persistent>
        <VCard>
            <VCardTitle>{{ $t("calendar.event.title") }}</VCardTitle>
            <VCardText>
                <VTextField v-model.trim="editForm.taskId" @keydown.enter.prevent="saveEdit" :label="$t('calendar.event.taskIdLabel')" autofocus />
                <VTextField v-model="editForm.startTime" class="mt-3" label="Start" type="datetime-local" />
                <VTextField v-model="editForm.endTime" class="mt-3" label="End" type="datetime-local" />
            </VCardText>
            <VCardActions>
                <VSpacer />
                <VBtn @click="cancelEdit" variant="text">{{ $t("action.cancel") }}</VBtn>
                <VBtn @click="saveEdit" color="primary">{{ $t("action.save") }}</VBtn>
            </VCardActions>
        </VCard>
    </VDialog>
</template>

<script setup lang="ts">
import type { TimeEntryContract, TimeEntryId } from "@/contracts/TimeEntryContract";

const store = useTimeEntryStore();
const { timeEntries } = storeToRefs(store);
const dateFormatter = useDate();
const notify = useNotify();

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

const editDialog = ref(false);
const editingId = ref<TimeEntryId | null>(null);
const editForm = ref({
    taskId: "",
    startTime: "",
    endTime: ""
});

const toDatetimeLocal = (date: Date) => {
    const pad = (n: number) => n.toString().padStart(2, "0");
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
};

const startEdit = (item: TimeEntryContract) => {
    editingId.value = item.id;
    editForm.value = {
        taskId: item.taskId,
        startTime: toDatetimeLocal(item.startTime),
        endTime: toDatetimeLocal(item.endTime)
    };
    editDialog.value = true;
};

const cancelEdit = () => {
    editDialog.value = false;
    editingId.value = null;
};

const saveEdit = async () => {
    if (!editingId.value) return;

    const result = await store.update(editingId.value, {
        taskId: editForm.value.taskId,
        startTime: new Date(editForm.value.startTime),
        endTime: new Date(editForm.value.endTime)
    });

    if (result.status === "success") {
        notify.success($t("action.save"));
        editDialog.value = false;
        editingId.value = null;
    } else {
        notify.error($t("action.save.error"));
    }
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
