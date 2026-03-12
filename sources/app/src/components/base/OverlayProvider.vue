<template>
    <VForm v-model="valid">
        <VMenu v-if="overlayType === OverlayType.Menu" v-model="overlayOpen" v-bind="$attrs" :closeOnContentClick="false" location="right" minWidth="350">
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
        <VDialog v-if="overlayType === OverlayType.Dialog" v-model="overlayOpen" v-bind="$attrs" :closeOnContentClick="false" width="800">
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
        <VNavigationDrawer v-if="overlayType === OverlayType.Drawer" v-model="overlayOpen" v-bind="$attrs" location="right" width="300" disableResizeWatcher>
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
        </VNavigationDrawer>
    </VForm>
</template>

<script setup lang="ts">
import { OverlayType } from "@/models/DisplaySettings";

const emit = defineEmits(["closed"]);

defineProps<{
    loading: boolean;
}>();

const overlayType = ref<OverlayType>(OverlayType.Menu);

const overlayOpen = ref(true);
const valid = ref(false);

whenever(
    () => !overlayOpen.value,
    () => emit("closed")
);
</script>
