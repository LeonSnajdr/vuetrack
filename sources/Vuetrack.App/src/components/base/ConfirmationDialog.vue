<template>
    <VDialog v-model="dialog" width="auto">
        <VCard>
            <VCardTitle>
                {{ $t("action.delete.title", { type }) }}
            </VCardTitle>
            <VCardText>
                {{ $t("action.delete.info") }}
                <slot name="text" />
            </VCardText>
            <VCardActions>
                <VSpacer />
                <VBtn @click="dialog = false">{{ $t("action.cancel") }}</VBtn>
                <VBtn @click="confirmClicked" color="error" variant="elevated">{{ $t("action.delete") }}</VBtn>
            </VCardActions>
        </VCard>
    </VDialog>
</template>

<script setup lang="ts">
const emit = defineEmits<{
    (e: "confirm"): void;
}>();

defineProps<{
    type: string;
}>();

const dialog = defineModel<boolean>({ default: false });

const confirmClicked = () => {
    dialog.value = false;
    emit("confirm");
};
</script>
