<template>
    <VForm v-model="valid">
        <VMenu
            v-if="overlayType === OverlayType.Menu"
            v-model="overlayOpen"
            v-bind="$attrs"
            :closeOnContentClick="false"
            :persistent="loading"
            location="right"
            minWidth="350"
        >
            <VCard>
                <VCardTitle>
                    <slot name="title" />
                </VCardTitle>
                <VCardText>
                    <slot name="content" />
                </VCardText>
                <VCardActions>
                    <VSpacer />
                    <VBtn @click="overlayOpen = false" :disabled="loading" variant="flat">{{ $t("action.cancel") }}</VBtn>
                    <slot :valid="valid" name="actions" />
                </VCardActions>
            </VCard>
        </VMenu>
        <VDialog v-if="overlayType === OverlayType.Dialog" v-model="overlayOpen" v-bind="$attrs" :closeOnContentClick="false" :persistent="loading" width="800">
            <VCard>
                <VCardTitle>
                    <slot name="title" />
                </VCardTitle>
                <VCardText>
                    <slot name="content" />
                </VCardText>
                <VCardActions>
                    <VSpacer />
                    <VBtn @click="overlayOpen = false" :disabled="loading" variant="flat">{{ $t("action.cancel") }}</VBtn>
                    <slot :valid="valid" name="actions" />
                </VCardActions>
            </VCard>
        </VDialog>
        <VNavigationDrawer
            v-if="overlayType === OverlayType.Drawer"
            v-model="overlayOpen"
            v-bind="$attrs"
            :persistent="loading"
            location="right"
            width="500"
            disableResizeWatcher
        >
            <VCard elevation="0">
                <VCardTitle>
                    <slot name="title" />
                </VCardTitle>
                <VCardText>
                    <slot name="content" />
                </VCardText>
                <VCardActions>
                    <VSpacer />
                    <VBtn @click="overlayOpen = false" :disabled="loading" variant="flat">{{ $t("action.cancel") }}</VBtn>
                    <slot :valid="valid" name="actions" />
                </VCardActions>
            </VCard>
        </VNavigationDrawer>
    </VForm>
</template>

<script setup lang="ts">
import { OverlayType } from "@/models/DisplaySettings";

const emit = defineEmits(["closed"]);

const props = defineProps<{
    loading: boolean;
}>();

const overlayOpen = defineModel<boolean>({ default: true });

const overlayType = ref<OverlayType>(OverlayType.Drawer);

const valid = ref(false);

useHotkey(
    "escape",
    () => {
        if (props.loading) return;
        overlayOpen.value = false;
    },
    { inputs: true }
);

whenever(
    () => !overlayOpen.value,
    () => emit("closed")
);
</script>
