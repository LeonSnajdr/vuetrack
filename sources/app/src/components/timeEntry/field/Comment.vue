<template>
    <VRowSingle>
        <VTextarea v-bind="$attrs" v-model.trim="comment" :label="$t('timeEntry.field.comment')" maxlength="1024" maxRows="5" rows="1" autoGrow />
    </VRowSingle>
</template>

<script setup lang="ts">
const comment = defineModel<string | null>({ required: true });
</script>
