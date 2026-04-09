<template>
    <BaseOverlayProvider @closed="create.cancel" :loading="isCreatingEvent" :target="targetSelector">
        <template #title>
            {{ $t("action.create.title", { type: $t("timeEntry.singular") }) }}
        </template>
        <template #content>
            <TimeEntryFieldContainer v-model="interaction.mutation.create" v-model:valid="valid" skipTimeFields />
        </template>
        <template #actions>
            <VBtn @click="create.finish" :disabled="!valid" :loading="isCreatingEvent" color="primary" variant="flat">
                {{ $t("action.create") }}
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
const valid = ref(false);

const targetSelector = computed(() => "#" + interaction.value.event.uiId);
</script>
