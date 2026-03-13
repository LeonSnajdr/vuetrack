<template>
    <BaseOverlayProvider @closed="emit('closed')" :loading="isCreating()" :target="targetSelector">
        <template #title>
            {{ $t("calendar.event.title") }}
        </template>
        <template #content>
            <TimeEntryFieldTaskId v-model="draftTimeEntry.taskId" density="compact" autofocus />
            <TimeEntryFieldStartTime v-model="draftTimeEntry.startTime" />
            <TimeEntryFieldEndTime v-model="draftTimeEntry.endTime" :startTime="draftTimeEntry.startTime" />
        </template>
        <template #actions="{ valid }">
            <VBtn @click="finish" :disabled="!valid" :loading="isCreating()" color="primary" variant="flat">
                {{ $t("action.add") }}
            </VBtn>
        </template>
    </BaseOverlayProvider>
</template>

<script setup lang="ts">
import type { TimeEntryCreateContract } from "@/contracts/TimeEntryContract";

const emit = defineEmits(["closed"]);

const props = defineProps<{
    timeEntryCreate: TimeEntryCreateContract;
}>();

const notify = useNotify();
const { t } = useI18n();
const timeEntryStore = useTimeEntryStore();

const { isCreating } = storeToRefs(timeEntryStore);
const draftTimeEntry = ref<TimeEntryCreateContract>({} as TimeEntryCreateContract);
const targetSelector = "#time-entry-create";

watch(
    () => props.timeEntryCreate,
    () => {
        draftTimeEntry.value = {
            taskId: props.timeEntryCreate.taskId,
            startTime: props.timeEntryCreate.startTime,
            endTime: props.timeEntryCreate.endTime
        };
    },
    { immediate: true }
);

const finish = async () => {
    const result = await timeEntryStore.create(draftTimeEntry.value);

    if (result.status === "error") {
        notify.error(t("action.add.error"), { timeout: 5000 });
    }

    emit("closed");
};
</script>
