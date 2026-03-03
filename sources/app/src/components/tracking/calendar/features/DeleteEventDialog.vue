<template>
    <VMenu @update:modelValue="(v) => !v && remove.cancel()" :closeOnContentClick="false" :target="targetSelector" location="right" modelValue>
        <VCard width="320">
            <VCardText class="pt-4">
                {{ $t("action.delete.info") }}
            </VCardText>
            <VCardActions>
                <VSpacer />
                <VBtn @click="remove.cancel()" @mousedown.stop variant="text">
                    {{ $t("action.cancel") }}
                </VBtn>
                <VBtn @click="remove.finish()" @mousedown.stop color="error">
                    {{ $t("action.delete") }}
                </VBtn>
            </VCardActions>
        </VCard>
    </VMenu>
</template>

<script setup lang="ts">
import type { Interaction } from "@/components/tracking/calendar/types";
import { useDelete } from "@/components/tracking/calendar/composables/useDelete";

const interaction = defineModel<Extract<Interaction, { kind: "delete" }>>("interaction", { required: true });
const remove = useDelete();

const targetSelector = computed(() => "#" + interaction.value.event.uiId);
</script>
