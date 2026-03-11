<template>
    <TimeEntryFeatureEdit
        v-model="interaction.mutation.update"
        :target="targetSelector"
        :loading="isUpdatingEvent"
        @update="edit.finish"
        @cancel="edit.cancel"
    />
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
