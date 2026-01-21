<template>
    <VMenu @update:modelValue="(v) => !v && create.cancel()" :closeOnContentClick="false" :target="targetSelector" location="right" modelValue>
        <VCard class="pa-3" width="320">
            <VCardTitle class="text-subtitle-1 pa-0">
                {{ $t("calendar.event.title") }}
            </VCardTitle>
            <VTextField
                v-model.trim="interaction.mutation.create.taskId"
                @keydown.enter.prevent="create.finish(interaction.event)"
                @keydown.esc.prevent="create.cancel()"
                :label="$t('calendar.event.taskIdLabel')"
                class="mt-3"
                density="compact"
                autofocus
            />
            <div class="d-flex justify-end ga-2 mt-2">
                <VBtn @click="create.cancel()" :disabled="createLoading" variant="text">
                    {{ $t("action.cancel") }}
                </VBtn>
                <VBtn @click="create.finish(interaction.event)" :disabled="createLoading" :loading="createLoading" color="primary">
                    {{ $t("action.save") }}
                </VBtn>
            </div>
        </VCard>
    </VMenu>
</template>

<script setup lang="ts">
import type { Interaction } from "@/components/tracking/calendar/types";
import { useCreate } from "@/components/tracking/calendar/composables/useCreate";

const interaction = defineModel<Extract<Interaction, { kind: "create" }>>("interaction", { required: true });

const create = useCreate();
const calendarStore = useCalendarStore();
const { createLoading } = storeToRefs(calendarStore);

const targetSelector = computed(() => "#" + interaction.value.event.uiId);
</script>
