<template>
    <VMenu
        @update:modelValue="(v) => !v && remove.cancel()"
        :closeOnContentClick="false"
        :persistent="isDeletingEvent"
        :target="targetSelector"
        location="right"
        modelValue
    >
        <VCard width="320">
            <VCardText class="pt-4">
                {{ $t("action.delete.info") }}
            </VCardText>
            <VCardActions>
                <VSpacer />
                <VBtn @click="remove.cancel()" :disabled="isDeletingEvent" variant="text">
                    {{ $t("action.cancel") }}
                </VBtn>
                <VBtn @click="remove.finish()" :loading="isDeletingEvent" color="error">
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
const calendarStore = useCalendarStore();
const { isDeletingEvent } = storeToRefs(calendarStore);

const targetSelector = computed(() => "#" + interaction.value.event.uiId);
</script>
