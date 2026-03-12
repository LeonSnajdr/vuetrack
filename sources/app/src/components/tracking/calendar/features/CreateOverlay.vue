<template>
    <BaseOverlayProvider @closed="create.cancel" :persistent="isCreatingEvent" :target="targetSelector">
        <template #title>
            {{ $t("calendar.event.title") }}
        </template>
        <template #content>
            <TimeEntryFieldTaskId v-model="interaction.mutation.create.taskId" density="compact" autofocus />
        </template>
        <template #actions="{ valid }">
            <VBtn @click="create.finish" :disabled="!valid" :loading="isCreatingEvent" color="primary" variant="flat">
                {{ $t("action.save") }}
            </VBtn>
        </template>
    </BaseOverlayProvider>
</template>

<script setup lang="ts">
import type { Interaction } from "@/components/tracking/calendar/types";
import { useCreate } from "@/components/tracking/calendar/composables/useCreate";

const interaction = defineModel<Extract<Interaction, { kind: "create" }>>("interaction", { required: true });

const create = useCreate();
const calendarStore = useCalendarStore();
const { isCreatingEvent } = storeToRefs(calendarStore);

const targetSelector = computed(() => "#" + interaction.value.event.uiId);
</script>
