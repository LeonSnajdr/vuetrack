<template>
    <div class="h-100 d-flex flex-column ga-4">
        <VCard class="border" variant="outlined">
            <VCardTitle>
                <VIcon :icon="mdiArrowExpandVertical" />
                {{ $t("calendar.intervalMinutes") }}
            </VCardTitle>
            <VCardText>
                <VSelect v-model="intervalMinutes" :items="intervalMinuteOptions" itemTitle="title" itemValue="value" />
            </VCardText>
        </VCard>
        <VCard class="border" variant="outlined">
            <VCardTitle>
                <VIcon :icon="mdiClockOutline" />
                {{ $t("calendar.dayRange") }}
            </VCardTitle>
            <VCardText class="pt-6">
                <VRangeSlider v-model="dayRange" :max="24" :min="0" :step="1" thumbLabel="always" strict>
                    <template #thumb-label="{ modelValue }">{{ formatHour(modelValue) }}</template>
                </VRangeSlider>
            </VCardText>
        </VCard>
    </div>
</template>

<script setup lang="ts">
import { useCalendarInterval } from "@/components/tracking/calendar/composables/useCalendarInterval";

const { intervalMinuteOptions, intervalMinutes, dayRange } = useCalendarInterval();

const formatHour = (hour: number) => `${String(hour).padStart(2, "0")}:00`;
</script>
