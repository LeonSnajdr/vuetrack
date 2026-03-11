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
import type { TimeEntryCreateContract } from "@/contracts/TimeEntryContract";

const timeEntry = defineModel<TimeEntryCreateContract>({ required: true });

const emit = defineEmits(["delete", "cancel"]);

const props = defineProps<{
    loading: boolean;
}>();

const form = useTemplateRef("form");

const finish = () => {
    if (!form.value?.isValid) return;
    emit("delete");
};

useHotkey("cmd+s", finish, { inputs: true });
</script>
