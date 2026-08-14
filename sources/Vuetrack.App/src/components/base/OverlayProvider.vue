<template>
    <VForm v-model="valid">
        <VMenu
            v-if="overlayType === OverlayType.Menu"
            v-model="overlayOpen"
            v-bind="$attrs"
            :closeOnContentClick="false"
            :contentProps="{ style: dragStyle }"
            :persistent="isPersistent"
            location="right"
            minWidth="350"
        >
            <VCard>
                <VCardTitle @mousedown="startDrag" class="cursor-move">
                    <VIcon v-if="interactive" :icon="mdiDragHorizontalVariant" class="mr-2" size="small" />
                    <slot name="title" />
                </VCardTitle>
                <VCardText>
                    <slot name="content" />
                </VCardText>
                <VCardActions>
                    <slot name="actionPrepend" />
                    <VSpacer />
                    <VBtn @click="overlayOpen = false" :disabled="loading" variant="flat">{{ $t("action.cancel") }}</VBtn>
                    <slot :valid="valid" name="actions" />
                </VCardActions>
            </VCard>
        </VMenu>
        <VDialog
            v-if="overlayType === OverlayType.Dialog"
            v-model="overlayOpen"
            v-bind="$attrs"
            :closeOnContentClick="false"
            :contentProps="{ style: dragStyle }"
            :persistent="isPersistent"
            :scrim="!interactive"
            width="800"
        >
            <VCard>
                <VCardTitle @mousedown="startDrag" class="cursor-move">
                    <VIcon v-if="interactive" :icon="mdiDragHorizontalVariant" class="mr-2" size="small" />
                    <slot name="title" />
                </VCardTitle>
                <VCardText>
                    <slot name="content" />
                </VCardText>
                <VCardActions>
                    <slot name="actionPrepend" />
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
            <VCard class="h-100" elevation="0">
                <VCardTitle>
                    <slot name="title" />
                </VCardTitle>
                <VCardText>
                    <slot name="content" />
                </VCardText>
                <VCardActions>
                    <slot name="actionPrepend" />
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

const emit = defineEmits(["closed", "submit"]);

const props = defineProps<{
    loading: boolean;
    interactive?: boolean;
}>();

const overlayOpen = defineModel<boolean>({ default: true });

const settingsStore = useSettingsStore();
const { generalSettings } = storeToRefs(settingsStore);
const overlayType = computed<OverlayType>(() => generalSettings.value.overlayType);
const isPersistent = computed(() => props.loading || props.interactive === true);

const { style: dragStyle, start: beginDrag } = useDraggableOverlay();

const valid = ref(false);

const startDrag = (nativeEvent: MouseEvent) => {
    if (!props.interactive) return;
    beginDrag(nativeEvent);
};

useHotkey(
    "escape",
    (e) => {
        e.preventDefault();
        if (props.loading) return;
        overlayOpen.value = false;
    },
    { inputs: true }
);

useHotkey(
    "cmd+s",
    (e) => {
        e.preventDefault();
        if (props.loading) return;
        emit("submit");
    },
    { inputs: true }
);

whenever(
    () => !overlayOpen.value,
    () => emit("closed")
);
</script>
