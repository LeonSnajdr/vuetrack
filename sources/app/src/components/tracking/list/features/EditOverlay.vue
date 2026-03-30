<template>
    <BaseOverlayProvider @closed="edit.cancel" :loading="isUpdatingEntry" :target="targetSelector">
        <template #title>
            {{ $t("action.save.title", { type: $t("timeEntry.singular") }) }}
        </template>
        <template #content>
            <TimeEntryFieldContainer v-model="interaction.update" v-model:valid="valid" />
        </template>
        <template #actions>
            <VBtn @click="edit.finish" :disabled="!valid" :loading="isUpdatingEntry" color="primary" variant="flat">
                {{ $t("action.save") }}
            </VBtn>
        </template>
    </BaseOverlayProvider>
</template>

<script setup lang="ts">
import type { Interaction } from "@/components/tracking/list/types";
import { useEdit } from "@/components/tracking/list/composables/useEdit";

const interaction = defineModel<Extract<Interaction, { kind: "edit" }>>("interaction", { required: true });

const edit = useEdit();
const listStore = useTrackingListStore();
const { isUpdatingEntry } = storeToRefs(listStore);
const targetSelector = computed(() => "#time-entry-edit-" + interaction.value.timeEntryId);
const valid = ref(false);
</script>
