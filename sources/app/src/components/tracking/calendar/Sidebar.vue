<template>
    <VNavigationDrawer location="left" width="280" permanent>
        <div class="pa-4 d-flex flex-column ga-4">
            <VSelect
                v-model="intervalMinutes"
                :items="intervalMinuteOptions"
                :label="$t('calendar.intervalMinutes')"
                density="compact"
                itemTitle="title"
                itemValue="value"
            />
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
import { useCalendarInterval } from "./composables/useCalendarInterval";

const calendarStore = useCalendarStore();
const { preselectedTaskId } = storeToRefs(calendarStore);
const { intervalMinuteOptions, intervalMinutes } = useCalendarInterval();
</script>
