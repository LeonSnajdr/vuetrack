<template>
    <TimeEntryFeatureDelete :target="targetSelector" :loading="isDeletingEvent" @delete="remove.finish" @cancel="remove.cancel" />
</template>

<script setup lang="ts">
import type { Interaction } from "@/components/tracking/calendar/types";
import { useDelete } from "@/components/tracking/calendar/composables/useDelete";

const interaction = defineModel<Extract<Interaction, { kind: "delete" }>>("interaction", { required: true });
const remove = useDelete();
const calendarStore = useCalendarStore();
const { isDeletingEvent } = storeToRefs(calendarStore);

const targetSelector = computed(() => "#" + interaction.value.event.uiId);
</script>
