<template>
    <VSelect
        v-model="intervalMinutes"
        :items="intervalMinuteOptions"
        :prependIcon="mdiArrowExpandVertical"
        density="compact"
        itemTitle="title"
        itemValue="value"
        variant="plain"
        v-tooltip="$t('calendar.intervalMinutes')"
    />
</template>
<script setup lang="ts">
import { useCalendarInterval } from "./composables/useCalendarInterval";

const { intervalMinuteOptions, intervalMinutes } = useCalendarInterval();
</script>
