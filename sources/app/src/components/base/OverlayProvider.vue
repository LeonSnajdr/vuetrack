<template>
    <VMenu
        v-if="overlayType === OverlayType.Menu"
        @update:modelValue="(v) => !v && emit('closed')"
        v-bind="$attrs"
        :closeOnContentClick="false"
        location="right"
        modelValue
    >
        <slot />
    </VMenu>
    <VDialog
        v-if="overlayType === OverlayType.Dialog"
        @update:modelValue="(v) => !v && emit('closed')"
        v-bind="$attrs"
        :closeOnContentClick="false"
        width="800"
        modelValue
    >
        <slot />
    </VDialog>
    <VNavigationDrawer
        v-if="overlayType === OverlayType.Drawer"
        @update:modelValue="(v) => !v && emit('closed')"
        v-bind="$attrs"
        location="right"
        width="300"
        disableResizeWatcher
        modelValue
    >
        <slot />
    </VNavigationDrawer>
</template>

<script setup lang="ts">
import { OverlayType } from "@/models/DisplaySettings";

const emit = defineEmits(["closed"]);

const overlayType = ref<OverlayType>(OverlayType.Drawer);
</script>
