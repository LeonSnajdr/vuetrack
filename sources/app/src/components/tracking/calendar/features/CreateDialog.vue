<template>
    <VMenu @update:modelValue="(v) => !v && emit('cancel')" :closeOnContentClick="false" :modelValue="open" :target="targetSelector" location="right">
        <VCard class="pa-3" width="320">
            <VCardTitle class="text-subtitle-1 pa-0">Name the event</VCardTitle>
            <template v-if="event?.kind === 'draft'">
                <VTextField
                    v-model.trim="event.createEntry.taskId"
                    @keydown.enter.prevent="confirm"
                    @keydown.esc.prevent="emit('cancel')"
                    class="mt-3"
                    density="compact"
                    label="Taskid"
                    autofocus
                />
            </template>
            <template v-if="event?.kind === 'suggestion'">
                <VTextField
                    v-model.trim="event.timeEntry.taskId"
                    @keydown.enter.prevent="confirm"
                    @keydown.esc.prevent="emit('cancel')"
                    class="mt-3"
                    density="compact"
                    label="Taskid"
                    autofocus
                />
            </template>
            <div class="d-flex justify-end ga-2 mt-2">
                <VBtn @click="emit('cancel')" variant="text">Cancel</VBtn>
                <VBtn @click="confirm" color="primary">Save</VBtn>
            </div>
        </VCard>
    </VMenu>
</template>

<script setup lang="ts">
import type { DraftTimeEntryEvent, SuggestionTimeEntryEvent } from "@/components/tracking/calendar/types";

const emit = defineEmits<{
    confirm: [event: DraftTimeEntryEvent | SuggestionTimeEntryEvent];
    cancel: [];
}>();

defineProps<{
    targetSelector: string;
}>();

const open = defineModel<boolean>({ required: true });
const event = defineModel<DraftTimeEntryEvent | SuggestionTimeEntryEvent | null>("event", { required: true });

const confirm = () => {
    if (event.value) {
        emit("confirm", event.value);
    }
};
</script>
