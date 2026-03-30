<template>
    <BaseOverlayProvider @closed="edit.cancel" :loading="isUpdatingEvent" :target="targetSelector">
        <template #title>
            {{ $t("action.save.title", { type: $t("timeEntry.singular") }) }}
        </template>
        <template #content>
            <TimeEntryFieldContainer v-model="interaction.mutation.update" v-model:valid="valid" />
        </template>
        <template #actions>
            <VBtn @click="edit.finish" :disabled="!valid" :loading="isUpdatingEvent" color="primary" variant="flat">
                {{ $t("action.save") }}
            </VBtn>
        </template>
    </BaseOverlayProvider>
</template>

<script setup lang="ts">
import type { Interaction } from "@/components/tracking/calendar/types";
import { useEdit } from "@/components/tracking/calendar/composables/useEdit";

const interaction = defineModel<Extract<Interaction, { kind: "edit" }>>("interaction", { required: true });

const edit = useEdit();
const calendarStore = useCalendarStore();
const { isUpdatingEvent } = storeToRefs(calendarStore);
const valid = ref(false);

const targetSelector = computed(() => "#" + interaction.value.event.uiId);
</script>
