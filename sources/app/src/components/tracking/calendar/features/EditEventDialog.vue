<template>
    <VMenu
        @update:modelValue="(v) => !v && edit.cancel()"
        :closeOnContentClick="false"
        :persistent="isUpdatingEvent"
        :target="targetSelector"
        location="right"
        modelValue
    >
        <VCard class="pa-3" width="320">
            <VCardTitle class="text-subtitle-1 pa-0">
                {{ $t("calendar.event.title") }}
            </VCardTitle>
            <TimeEntryFieldTaskId
                v-model="interaction.mutation.update.taskId"
                @keydown.enter.prevent="edit.finish()"
                @keydown.esc.prevent="edit.cancel()"
                class="mt-3"
                density="compact"
                autofocus
            />
            <VBtn @click="interaction.mutation.update.endTime = new Date(interaction.event.end + 60 * 60 * 1000)"> +1h </VBtn>
            <div class="d-flex justify-end ga-2 mt-2">
                <VBtn @click="edit.cancel()" :disabled="isUpdatingEvent" variant="text">
                    {{ $t("action.cancel") }}
                </VBtn>
                <VBtn @click="edit.finish()" :disabled="isUpdatingEvent" :loading="isUpdatingEvent" color="primary">
                    {{ $t("action.save") }}
                </VBtn>
            </div>
        </VCard>
    </VMenu>
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
