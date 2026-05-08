<template>
    <div class="h-100">
        <VCard class="border" variant="outlined">
            <VCardTitle>
                <VIcon :icon="mdiArrowExpandVertical" />
                {{ $t("calendar.intervalMinutes") }}
            </VCardTitle>
            <VCardText>
                <VSelect v-model="intervalMinutes" :items="intervalMinuteOptions" itemTitle="title" itemValue="value" />
            </VCardText>
        </VCard>
    </div>
</template>

<script setup lang="ts">
import { useCalendarInterval } from "@/components/tracking/calendar/composables/useCalendarInterval";

const { intervalMinuteOptions, intervalMinutes } = useCalendarInterval();
</script>
