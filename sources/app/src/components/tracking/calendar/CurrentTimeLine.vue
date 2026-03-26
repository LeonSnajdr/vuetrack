<template>
    <div v-if="isVisible" :style="lineStyle" aria-hidden="true" class="current-time-line">
        <div v-if="isCurrentDay" class="current-time-line__dot" />
    </div>
</template>

<script setup lang="ts">
import type { CalendarDayBodySlotScope } from "vuetify/lib/components/VCalendar/types.mjs";

const props = defineProps<{
    day: CalendarDayBodySlotScope;
}>();

const now = useNow({ interval: 60_000 });

const currentMinutes = computed(() => now.value.getHours() * 60 + now.value.getMinutes());

const isCurrentDay = computed(() => {
    return props.day.year === now.value.getFullYear() && props.day.month === now.value.getMonth() + 1 && props.day.day === now.value.getDate();
});

const isWithinVisibleRange = computed(() => {
    if (!props.day.intervalRange) return true;

    const [startMinutes, endMinutes] = props.day.intervalRange;
    return currentMinutes.value >= startMinutes && currentMinutes.value <= endMinutes;
});

const isVisible = computed(() => {
    return isCurrentDay.value && isWithinVisibleRange.value;
});

const lineStyle = computed(() => {
    const hours = String(now.value.getHours()).padStart(2, "0");
    const minutes = String(now.value.getMinutes()).padStart(2, "0");

    return {
        top: `${props.day.timeToY(`${hours}:${minutes}`)}px`
    };
});
</script>

<style scoped>
.current-time-line {
    position: absolute;
    left: 0;
    right: 0;
    border-top: 2px solid rgb(var(--v-theme-error));
    pointer-events: none;
    z-index: 3;
}

.current-time-line__dot {
    position: absolute;
    top: -5px;
    left: -1px;
    width: 10px;
    height: 10px;
    border-radius: 999px;
    background-color: rgb(var(--v-theme-error));
}
</style>
