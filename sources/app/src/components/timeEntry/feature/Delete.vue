<template>
    <BaseOverlayProvider @closed="emit('cancel')" :persistent="loading">
        <VCard>
            <VCardTitle>
                {{ $t("action.delete.info") }}
            </VCardTitle>
            <VCardActions>
                <VSpacer />
                <VBtn @click="emit('cancel')" :disabled="loading">
                    {{ $t("action.cancel") }}
                </VBtn>
                <VBtn @click="emit('delete')" :disabled="loading" :loading="loading" color="error" variant="flat">
                    {{ $t("action.delete") }}
                </VBtn>
            </VCardActions>
        </VCard>
    </BaseOverlayProvider>
</template>

<script setup lang="ts">
const emit = defineEmits(["delete", "cancel"]);

defineProps<{
    loading: boolean;
}>();
</script>
