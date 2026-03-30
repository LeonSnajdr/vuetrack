<template>
    <BaseOverlayProvider @closed="remove.cancel" :loading="isDeletingEntry" :target="targetSelector">
        <template #title>
            {{ $t("action.delete.title", { type: $t("timeEntry.singular") }) }}
        </template>
        <template #content>
            {{ $t("action.delete.info") }}
        </template>
        <template #actions>
            <VBtn @click="finish" :disabled="isDeletingEntry" :loading="isDeletingEntry" color="error" variant="flat">
                {{ $t("action.delete") }}
            </VBtn>
        </template>
    </BaseOverlayProvider>
</template>

<script setup lang="ts">
import type { Interaction } from "@/components/tracking/list/types";
import { useDelete } from "@/components/tracking/list/composables/useDelete";

const interaction = defineModel<Extract<Interaction, { kind: "delete" }>>("interaction", { required: true });

const remove = useDelete();
const listStore = useTrackingListStore();
const { isDeletingEntry } = storeToRefs(listStore);
const targetSelector = computed(() => "#time-entry-delete-" + interaction.value.timeEntryId);

const finish = async () => {
    await remove.finish();
};
</script>
