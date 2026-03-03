<template>
    <VNavigationDrawer location="left" width="280" permanent>
        <div class="pa-4">
            <VTextField
                v-model.trim="preselectedTaskId"
                :hint="$t('sidebar.preselectedTaskId.hint')"
                :label="$t('sidebar.preselectedTaskId')"
                density="compact"
                variant="outlined"
                clearable
                persistentHint
            />
        </div>
    </VNavigationDrawer>
</template>

<script setup lang="ts">
const calendarStore = useCalendarStore();
const { preselectedTaskId } = storeToRefs(calendarStore);
</script>
