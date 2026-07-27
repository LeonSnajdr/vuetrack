<template>
    <BaseOverlayProvider @closed="create.cancel" @submit="submit" :loading="isCreatingEvent" :target="targetSelector">
        <template #title>
            {{ $t("action.create.title", { type: $t("timeEntry.singular") }) }}
        </template>
        <template #content>
            <TimeEntryFieldContainer v-model="interaction.mutation.create" v-model:errors="interaction.errors" v-model:valid="valid" skipTimeFields />
        </template>
        <template #actions>
            <VBtn @click="submit" :disabled="!valid" :loading="isCreatingEvent" color="primary" variant="flat">
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

const submit = (): void => {
    if (!valid.value) return;
    if (isCreatingEvent.value) return;
    create.finish();
};
</script>
