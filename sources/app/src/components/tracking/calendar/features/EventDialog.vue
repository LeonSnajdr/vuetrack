<template>
    <VMenu @update:modelValue="(v) => !v && emit('cancel')" :closeOnContentClick="false" :target="targetSelector" location="right" modelValue>
        <VCard class="pa-3" width="320">
            <VCardTitle class="text-subtitle-1 pa-0">Name the event</VCardTitle>
            <VTextField
                v-if="event.kind === 'draft'"
                v-model.trim="event.createEntry.taskId"
                @keydown.enter.prevent="confirm"
                @keydown.esc.prevent="emit('cancel')"
                class="mt-3"
                density="compact"
                label="Taskid"
                autofocus
            />
            <VTextField
                v-else
                v-model.trim="event.timeEntry.taskId"
                @keydown.enter.prevent="confirm"
                @keydown.esc.prevent="emit('cancel')"
                class="mt-3"
                density="compact"
                label="Taskid"
                autofocus
            />
            <div class="d-flex justify-end ga-2 mt-2">
                <VBtn @click="emit('cancel')" :disabled="loading" variant="text">Cancel</VBtn>
                <VBtn @click="confirm" :disabled="loading" :loading="loading" color="primary">Save</VBtn>
            </div>
        </VCard>
    </VMenu>
</template>

<script setup lang="ts">
import type { DraftTimeEntryEvent, ExistingTimeEntryEvent, SuggestionTimeEntryEvent } from "@/components/tracking/calendar/types";

type EventType = DraftTimeEntryEvent | ExistingTimeEntryEvent | SuggestionTimeEntryEvent;

const emit = defineEmits<{
    confirm: [event: EventType];
    cancel: [];
}>();

defineProps<{
    loading: boolean;
}>();

const event = defineModel<EventType>("event", { required: true });

const targetSelector = computed(() => "#" + event.value.uiId);

const confirm = () => {
    emit("confirm", event.value);
};
</script>
