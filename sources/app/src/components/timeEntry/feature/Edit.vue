<template>
    <BaseOverlayProvider @closed="emit('cancel')" :persistent="loading">
        <VForm ref="form">
            <VCard>
                <VCardTitle>
                    {{ $t("calendar.event.title") }}
                </VCardTitle>
                <VCardText>
                    <TimeEntryFieldTaskId v-model="timeEntry.taskId" density="compact" autofocus />
                </VCardText>
                <VCardActions>
                    <VSpacer />
                    <VBtn @click="emit('cancel')" :disabled="loading">
                        {{ $t("action.cancel") }}
                    </VBtn>
                    <VBtn @click="finish()" :disabled="loading || !form?.isValid" :loading="loading" color="primary" variant="flat">
                        {{ $t("action.save") }}
                    </VBtn>
                </VCardActions>
            </VCard>
        </VForm>
    </BaseOverlayProvider>
</template>

<script setup lang="ts">
import type { TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";
import type { TimeEntrySuggestionUpdateContract } from "@/contracts/TimeEntrySuggestion";

const emit = defineEmits(["delete", "cancel"]);

defineProps<{
    loading: boolean;
}>();

const timeEntry = defineModel<TimeEntryUpdateContract | TimeEntrySuggestionUpdateContract>({ required: true });

const form = useTemplateRef("form");

const finish = () => {
    if (!form.value?.isValid) return;
    emit("delete");
};

useHotkey("cmd+s", finish, { inputs: true });
</script>
