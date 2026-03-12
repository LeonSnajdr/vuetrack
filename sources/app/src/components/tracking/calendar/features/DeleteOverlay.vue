<template>
    <BaseOverlayProvider @closed="remove.cancel" :loading="isDeletingEvent" :persistent="isDeletingEvent" :target="targetSelector">
        <template #title>
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
import type { Interaction } from "@/components/tracking/calendar/types";
import { useDelete } from "@/components/tracking/calendar/composables/useDelete";

const interaction = defineModel<Extract<Interaction, { kind: "delete" }>>("interaction", { required: true });
const remove = useDelete();
const calendarStore = useCalendarStore();
const { isDeletingEvent } = storeToRefs(calendarStore);

const targetSelector = computed(() => "#" + interaction.value.event.uiId);
</script>
