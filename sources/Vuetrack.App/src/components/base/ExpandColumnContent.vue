<template>
    <div class="text-break py-2">
        <span :class="{ 'text-pre-wrap': lineBreaks }">{{ collapsed ? truncatedText : text }}</span>

        <div v-if="isTruncated" @click.stop="collapsed = !collapsed" class="text-grey">
            <span>{{ collapsed ? $t("action.expand") : $t("action.collapse") }}</span>
            <VIcon :icon="collapsed ? mdiChevronDown : mdiChevronUp" size="small" />
        </div>
    </div>
</template>

<script setup lang="ts">
import { truncate } from "lodash";

const props = defineProps<{ text: string; length: number; lineBreaks?: boolean }>();

const collapsed = ref(true);

const truncatedText = computed(() => {
    return truncate(props.text, { length: props.length });
});

const isTruncated = computed(() => {
    return truncatedText.value !== props.text;
});
</script>
