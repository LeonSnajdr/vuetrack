<template>
    <BaseOverlayProvider @closed="remove.cancel" :loading="isDeletingEvent" :target="targetSelector">
        <template #title>
            {{ $t("action.delete.title", { type: $t("timeEntry.singular") }) }}
        </template>
        <template #content>
            {{ $t("action.delete.info") }}
        </template>
        <template #actions>
            <VBtn @click="remove.finish" :disabled="isDeletingEvent" :loading="isDeletingEvent" color="error" variant="flat">
                {{ $t("action.delete") }}
            </VBtn>
        </template>
    </BaseOverlayProvider>
</template>

<script setup lang="ts">
import type { Task } from "@/components/tracking/calendar/types";
import { useDelete } from "@/components/tracking/calendar/composables/useDelete";

const task = defineModel<Extract<Task, { kind: "delete" }>>("task", { required: true });
const remove = useDelete();
const calendarStore = useCalendarStore();
const { isDeletingEvent } = storeToRefs(calendarStore);

const targetSelector = computed(() => "#" + task.value.event.uiId);
</script>
