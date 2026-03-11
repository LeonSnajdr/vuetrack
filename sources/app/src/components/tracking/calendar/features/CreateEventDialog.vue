<template>
    <VMenu
        @update:modelValue="(v) => !v && create.cancel()"
        :closeOnContentClick="false"
        :persistent="isCreatingEvent"
        :target="targetSelector"
        location="right"
        modelValue
    >
        <VForm ref="form">
            <VCard width="320">
                <VCardTitle>
                    {{ $t("calendar.event.title") }}
                </VCardTitle>
                <VCardText>
                    <TimeEntryFieldTaskId v-model="interaction.mutation.create.taskId" density="compact" autofocus />
                </VCardText>
                <VCardActions>
                    <VSpacer />
                    <VBtn @click="create.cancel()" :disabled="isCreatingEvent" variant="text">
                        {{ $t("action.cancel") }}
                    </VBtn>
                    <VBtn @click="finish()" :disabled="isCreatingEvent || !form?.isValid" :loading="isCreatingEvent" color="primary">
                        {{ $t("action.save") }}
                    </VBtn>
                </VCardActions>
            </VCard>
        </VForm>
    </VMenu>
</template>

<script setup lang="ts">
import type { Interaction } from "@/components/tracking/calendar/types";
import { useCreate } from "@/components/tracking/calendar/composables/useCreate";

const interaction = defineModel<Extract<Interaction, { kind: "create" }>>("interaction", { required: true });

const create = useCreate();
const form = useTemplateRef("form");

const calendarStore = useCalendarStore();

const { isCreatingEvent } = storeToRefs(calendarStore);

const targetSelector = computed(() => "#" + interaction.value.event.uiId);

const finish = () => {
    if (!form.value?.isValid) return;
    create.finish();
};

useHotkey("cmd+s", finish, { inputs: true });
</script>
