<template>
    <BaseOverlayProvider @closed="edit.cancel" @submit="submit" :loading="isUpdatingEvent" :target="targetSelector">
        <template #title>
            {{ $t("action.save.title", { type: $t("timeEntry.singular") }) }}
        </template>
        <template #content>
            <TimeEntryFieldContainer
                v-model="task.payload"
                v-model:errors="task.errors"
                v-model:valid="valid"
                :disableRequired="task.event.kind === 'suggestion'"
            />
        </template>
        <template #actions>
            <VBtn @click="submit" :disabled="!valid" :loading="isUpdatingEvent" color="primary" variant="flat">
                {{ $t("action.save") }}
            </VBtn>
        </template>
    </BaseOverlayProvider>
</template>

<script setup lang="ts">
import type { Task } from "@/components/tracking/calendar/types";
import { useEdit } from "@/components/tracking/calendar/composables/useEdit";

const task = defineModel<Extract<Task, { kind: "edit" }>>("task", { required: true });

const edit = useEdit();
const calendarStore = useCalendarStore();
const { isUpdatingEvent } = storeToRefs(calendarStore);
const valid = ref(false);

const targetSelector = computed(() => "#" + task.value.event.uiId);

const submit = (): void => {
    if (!valid.value) return;
    if (isUpdatingEvent.value) return;
    edit.finish();
};
</script>
