<template>
    <BaseOverlayProvider @closed="create.cancel" :loading="isCreatingEntry" :target="targetSelector">
        <template #title>
            {{ $t("action.create.title", { type: $t("timeEntry.singular") }) }}
        </template>
        <template #content>
            <TimeEntryFieldContainer v-model="interaction.create" v-model:valid="valid" />
        </template>
        <template #actionPrepend>
            <VCheckbox v-model="createAnother" :label="$t('action.createAnother')" />
        </template>
        <template #actions>
            <VBtn @click="create.finish(createAnother)" :disabled="!valid" :loading="isCreatingEntry" color="primary" variant="flat">
                {{ $t("action.create") }}
            </VBtn>
        </template>
    </BaseOverlayProvider>
</template>

<script setup lang="ts">
import type { Interaction } from "@/components/tracking/list/types";
import { useCreate } from "@/components/tracking/list/composables/useCreate";

const interaction = defineModel<Extract<Interaction, { kind: "create" }>>("interaction", { required: true });

const create = useCreate();
const listStore = useTrackingListStore();
const { isCreatingEntry } = storeToRefs(listStore);

const valid = ref(false);
const createAnother = ref(false);
const targetSelector = "#time-entry-create";
</script>
