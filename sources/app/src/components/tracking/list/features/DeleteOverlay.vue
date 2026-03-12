<template>
    <BaseOverlayProvider @closed="emit('closed')" :loading="isDeleting(timeEntry.id)">
        <template #title>
            {{ $t("action.delete.info") }}
        </template>
        <template #actions>
            <VBtn @click="finish" :disabled="isDeleting(timeEntry.id)" :loading="isDeleting(timeEntry.id)" color="error" variant="flat">
                {{ $t("action.delete") }}
            </VBtn>
        </template>
    </BaseOverlayProvider>
</template>

<script setup lang="ts">
import type { TimeEntryContract } from "@/contracts/TimeEntryContract";

const emit = defineEmits(["closed"]);

const props = defineProps<{
    timeEntry: TimeEntryContract;
}>();

const notify = useNotify();
const { t } = useI18n();
const timeEntryStore = useTimeEntryStore();

const { isDeleting } = storeToRefs(timeEntryStore);

const finish = async () => {
    const result = await timeEntryStore.remove(props.timeEntry.id);

    if (result.status === "error") {
        notify.error(t("action.delete.error"), { timeout: 5000 });
    }

    emit("closed");
};
</script>
