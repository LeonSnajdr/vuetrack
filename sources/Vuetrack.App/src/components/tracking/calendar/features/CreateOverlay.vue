<template>
    <BaseOverlayProvider @closed="create.cancel" @submit="submit" :loading="isCreatingEvent" :target="targetSelector">
        <template #title>
            {{ $t("action.create.title", { type: $t("timeEntry.singular") }) }}
        </template>
        <template #content>
            <TimeEntryFieldContainer v-model="task.payload" v-model:errors="task.errors" v-model:valid="valid" skipTimeFields />
        </template>
        <template #actions>
            <VBtn @click="submit" :disabled="!valid" :loading="isCreatingEvent" color="primary" variant="flat">
                {{ $t("action.create") }}
            </VBtn>
        </template>
    </BaseOverlayProvider>
</template>

<script setup lang="ts">
import type { Task } from "@/components/tracking/calendar/types";
import { useCreate } from "@/components/tracking/calendar/composables/useCreate";

const task = defineModel<Extract<Task, { kind: "create" }>>("task", { required: true });

const create = useCreate();
const calendarStore = useCalendarStore();
const { isCreatingEvent } = storeToRefs(calendarStore);
const valid = ref(false);

const targetSelector = computed(() => "#" + task.value.event.uiId);

const submit = (): void => {
    if (!valid.value) return;
    if (isCreatingEvent.value) return;
    create.finish();
};
</script>
