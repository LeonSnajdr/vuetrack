<template>
    <VEmptyState :icon="mdiClockOutline" :text="$t('tracking.empty.text')" :title="$t('tracking.empty.title')">
        <template #actions>
            <VBtn @click="settingsDialogStore.open('backends')" :prependIcon="mdiConnection" color="primary" variant="flat">
                {{ $t("tracking.empty.action") }}
            </VBtn>
        </template>
    </VEmptyState>
</template>

<script setup lang="ts">
const settingsDialogStore = useSettingsDialogStore();
</script>
