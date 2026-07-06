<template>
    <VToolbar class="px-4 bg-surface" flat>
        <TrackingTimePeriodSelection />
        <div id="tracking-toolbar-prepend" class="d-inline-flex align-center ga-2 ml-2" />
        <template #append>
            <div class="d-flex align-center ga-2">
                <div id="tracking-toolbar-append" class="d-inline-flex align-center ga-2" />
                <VBtn @click="timeEntrySuggestionStore.recommendAgain()" :icon="mdiLightbulbOnOutline" density="comfortable" variant="text">
                    <VIcon :icon="mdiLightbulbOnOutline" />
                    <VTooltip activator="parent" location="bottom">{{ $t("suggestion.recommendAgain") }}</VTooltip>
                </VBtn>
                <VBtnToggle density="compact" variant="outlined">
                    <VBtn :to="{ name: '/tracking/calendar', query: $route.query }"> Calendar </VBtn>
                    <VBtn :to="{ name: '/tracking/list', query: $route.query }"> List</VBtn>
                </VBtnToggle>
            </div>
        </template>
    </VToolbar>
</template>

<script setup lang="ts">
const timeEntrySuggestionStore = useTimeEntrySuggestionStore();
</script>
