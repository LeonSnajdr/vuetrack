<template>
    <TimeEntryFeatureCreate
        v-model="interaction.mutation.create"
        :target="targetSelector"
        :loading="isCreatingEvent"
        @create="create.finish"
        @cancel="create.cancel"
    ></TimeEntryFeatureCreate>
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
