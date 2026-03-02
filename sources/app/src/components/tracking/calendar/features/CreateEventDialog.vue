<template>
    <VMenu @update:modelValue="(v) => !v && create.cancel()" :closeOnContentClick="false" :target="targetSelector" location="right" modelValue>
        <VCard width="320">
            <VCardTitle>
                {{ $t("calendar.event.title") }}
            </VCardTitle>
            <VCardText>
                <TimeEntryFieldTaskId
                    v-model="interaction.mutation.create.taskId"
                    @keydown.enter.prevent="create.finish()"
                    @keydown.esc.prevent="create.cancel()"
                    density="compact"
                    autofocus
                />
            </VCardText>
            <VCardActions>
                <VSpacer />
                <VBtn @click="create.cancel()" :disabled="createLoading" variant="text">
                    {{ $t("action.cancel") }}
                </VBtn>
                <VBtn @click="create.finish()" :disabled="createLoading" :loading="createLoading" color="primary">
                    {{ $t("action.save") }}
                </VBtn>
            </VCardActions>
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
