<template>
    <VRowSingle>
        <VTextField v-bind="$attrs" v-model.trim="comment" :label="$t('timeEntry.field.comment')" />
    </VRowSingle>
</template>

<script setup lang="ts">
const comment = defineModel<string>({ required: true });
</script>
