<template>
    <VNavigationDrawer location="left" width="280" permanent>
        <div class="pa-4 d-flex flex-column ga-4">
            <VTextField
                v-model.trim="preselectedTaskId"
                :hint="$t('sidebar.preselectedTaskId.hint')"
                :label="$t('sidebar.preselectedTaskId')"
                density="compact"
                clearable
                persistentHint
            />
        </div>
    </VNavigationDrawer>
</template>

<script setup lang="ts">
const trackingStore = useTrackingStore();
const { preselectedTaskId } = storeToRefs(trackingStore);
</script>
