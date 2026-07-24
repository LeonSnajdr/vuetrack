<template>
    <Render />
</template>

<script setup lang="ts">
const emit = defineEmits<{
    (e: "rulesChanged"): void;
}>();

const props = defineProps<{
    rules: unknown;
}>();

watch(
    () => props.rules,
    () => {
        emit("rulesChanged");
    },
    { deep: true }
);
const Render = () => {
    return null;
};
</script>
