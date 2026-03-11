<template>
    <TimeEntryFeatureEdit v-model="interaction.mutation.update" @cancel="edit.cancel" @edit="edit.finish" :loading="isUpdatingEvent" :target="targetSelector" />
</template>

<script setup lang="ts">
import type { Interaction } from "@/components/tracking/calendar/types";
import { useEdit } from "@/components/tracking/calendar/composables/useEdit";

const interaction = defineModel<Extract<Interaction, { kind: "edit" }>>("interaction", { required: true });

const edit = useEdit();
const calendarStore = useCalendarStore();
const { isUpdatingEvent } = storeToRefs(calendarStore);

const targetSelector = computed(() => "#" + interaction.value.event.uiId);
</script>
