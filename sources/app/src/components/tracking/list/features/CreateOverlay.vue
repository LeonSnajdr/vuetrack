<template>
    <BaseOverlayProvider @closed="emit('closed')" :loading="isCreating()" :target="targetSelector">
        <template #title>
            {{ $t("action.create.title", { type: $t("timeEntry.singular") }) }}
        </template>
        <template #content>
            <TimeEntryFieldContainer v-model="draftTimeEntry" v-model:valid="valid" />
        </template>
        <template #actionPrepend>
            <VCheckbox v-model="createAnother" label="Weiteres erstellen" />
        </template>
        <template #actions>
            <VBtn @click="finish" :disabled="!valid" :loading="isCreating()" color="primary" variant="flat">
                {{ $t("action.create") }}
            </VBtn>
        </template>
    </BaseOverlayProvider>
</template>

<script setup lang="ts">
import type { TimeEntryCreateContract } from "@/contracts/TimeEntryContract";

const emit = defineEmits(["closed"]);

const notify = useNotify();
const { t } = useI18n();
const timeEntryStore = useTimeEntryStore();

const { isCreating } = storeToRefs(timeEntryStore);
const valid = ref(false);
const createAnother = ref(false);
const draftTimeEntry = ref<TimeEntryCreateContract>({} as TimeEntryCreateContract);
const targetSelector = "#time-entry-create";

onBeforeMount(() => createDefaultTimeEntry());

const createDefaultTimeEntry = () => {
    draftTimeEntry.value = {
        taskId: "",
        startTime: new Date(),
        endTime: new Date(),
        activityId: null,
        projectId: null,
        comment: ""
    };
};

const finish = async () => {
    const result = await timeEntryStore.create(draftTimeEntry.value);

    if (result.status === "error") {
        notify.error(t("action.create.error", { type: t("timeEntry.singular") }), { timeout: 5000 });
    }

    if (result.status === "success" && createAnother.value) {
        createDefaultTimeEntry();
        return;
    }

    emit("closed");
};
</script>
