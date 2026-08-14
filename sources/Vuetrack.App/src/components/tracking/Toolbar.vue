<template>
    <VToolbar class="px-4 bg-surface" flat>
        <TrackingTimePeriodSelection />
        <div id="tracking-toolbar-prepend" class="d-inline-flex align-center ga-2 ml-2" />
        <template #append>
            <div class="d-flex align-center ga-2">
                <div id="tracking-toolbar-append" class="d-inline-flex align-center ga-2" />
                <VBtn
                    :icon="mdiLightbulbOnOutline"
                    :loading="timeEntrySuggestionStore.isGenerating || timeEntrySuggestionStore.isReloading"
                    density="comfortable"
                    variant="text"
                >
                    <VIcon :icon="mdiLightbulbOnOutline" />
                    <VMenu activator="parent" location="bottom end">
                        <VList density="compact">
                            <VListItem
                                @click="timeEntrySuggestionStore.generate()"
                                :prependIcon="mdiLightbulbOnOutline"
                                :subtitle="$t('suggestion.generate.hint')"
                                :title="$t('suggestion.generate')"
                            />
                            <VListItem
                                @click="timeEntrySuggestionStore.reload()"
                                :prependIcon="mdiRefresh"
                                :subtitle="$t('suggestion.reloadHint')"
                                :title="$t('suggestion.reload')"
                            />
                        </VList>
                    </VMenu>
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
