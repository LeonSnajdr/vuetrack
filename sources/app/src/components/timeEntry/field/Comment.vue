<template>
    <VRowSingle>
        <VTextarea v-bind="$attrs" v-model.trim="comment" :label="$t('timeEntry.field.comment')" maxRows="5" rows="1" autoGrow />
    </VRowSingle>
</template>

<script setup lang="ts">
const comment = defineModel<string>({ required: true });
</script>
