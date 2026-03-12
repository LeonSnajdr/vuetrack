<template>
    <BaseOverlayProvider @closed="emit('closed')" :loading="isUpdating(timeEntry.id)" :target="targetSelector">
        <template #title>
            {{ $t("calendar.event.title") }}
        </template>
        <template #content>
            <TimeEntryFieldTaskId v-model="timeEntryUpdate.taskId" />
            <TimeEntryFieldStartTime v-model="timeEntryUpdate.startTime" />
            <TimeEntryFieldEndTime v-model="timeEntryUpdate.endTime" />
        </template>
        <template #actions="{ valid }">
            <VBtn @click="finish" :disabled="!valid" :loading="isUpdating(timeEntry.id)" color="primary" variant="flat">
                {{ $t("action.save") }}
            </VBtn>
        </template>
    </BaseOverlayProvider>
</template>

<script setup lang="ts">
import type { TimeEntryContract, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";

const emit = defineEmits(["closed"]);

const props = defineProps<{
    timeEntry: TimeEntryContract;
}>();

const notify = useNotify();
const { t } = useI18n();
const timeEntryStore = useTimeEntryStore();

const { isUpdating } = storeToRefs(timeEntryStore);
const timeEntryUpdate = ref<TimeEntryUpdateContract>({} as TimeEntryUpdateContract);
const targetSelector = computed(() => "#time-entry-edit-" + props.timeEntry.id);

watch(
    () => props.timeEntry,
    () => {
        timeEntryUpdate.value = {
            taskId: props.timeEntry.taskId,
            endTime: props.timeEntry.endTime,
            startTime: props.timeEntry.startTime
        };
    },
    { immediate: true }
);

const finish = async () => {
    const result = await timeEntryStore.update(props.timeEntry.id, timeEntryUpdate.value);

    if (result.status === "error") {
        notify.error(t("action.save.error"), { timeout: 5000 });
    }

    emit("closed");
};
</script>
