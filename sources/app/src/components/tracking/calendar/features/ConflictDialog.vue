<template>
    <VMenu :closeOnContentClick="false" :target="targetSelector" location="right" modelValue persistent>
        <VCard class="pa-3" width="350">
            <VCardTitle class="text-subtitle-1 text-error pa-0 mb-2"> Overlap Detected </VCardTitle>
            <VCardSubtitle class="pa-0 mb-3"> This event overlaps with {{ overlaps.length }} other(s). </VCardSubtitle>

            <div class="d-flex flex-column ga-2">
                <VBtn
                    v-for="strategy in defaultStrategies"
                    :key="strategy.id"
                    @click="executeStrategy(strategy)"
                    :color="strategy.variant"
                    :disabled="loadingStrategyId !== null"
                    :loading="loadingStrategyId === strategy.id"
                    :prependIcon="strategy.icon"
                    variant="tonal"
                >
                    {{ strategy.label }}
                </VBtn>
            </div>

            <div class="d-flex justify-end mt-2 border-t pt-2">
                <VBtn @click="emit('canceled')" :disabled="loadingStrategyId !== null" size="small" variant="plain">Cancel</VBtn>
            </div>
        </VCard>
    </VMenu>
</template>

<script setup lang="ts">
import type { TimeEntryEvent } from "@/components/tracking/calendar/types";
import type { ConflictContext, ConflictResolutionResult, ConflictResolutionStrategy, EventMutation } from "./types";

const emit = defineEmits<{
    resolved: [result: ConflictResolutionResult];
    canceled: [];
}>();

const props = defineProps<{
    event: TimeEntryEvent;
    overlaps: TimeEntryEvent[];
    allEvents: TimeEntryEvent[];
}>();

const loadingStrategyId = defineModel<string | null>("loadingStrategyId", { required: true });

const targetSelector = computed(() => "#" + props.event.uiId);

const executeStrategy = (strategy: ConflictResolutionStrategy) => {
    loadingStrategyId.value = strategy.id;

    const ctx: ConflictContext = {
        event: props.event,
        overlaps: props.overlaps,
        allEvents: props.allEvents
    };

    const result = strategy.resolve(ctx);
    if (result) {
        emit("resolved", result);
    } else {
        loadingStrategyId.value = null;
        emit("canceled");
    }
};

// Default strategies
const resolveShiftUp = (ctx: ConflictContext): ConflictResolutionResult | null => {
    const { event, allEvents } = ctx;
    const duration = event.end - event.start;

    const sorted = [...allEvents].filter((e) => e.uiId !== event.uiId).sort((a, b) => a.start - b.start);

    const dayStart = new Date(event.start).setHours(0, 0, 0, 0);
    const dayEnd = new Date(event.start).setHours(23, 59, 59, 999);
    const dayEvents = sorted.filter((e) => e.end > dayStart && e.start < dayEnd);

    let searchTime = event.start;
    let foundStart = -1;

    while (searchTime > dayStart) {
        const potentialStart = searchTime;
        const potentialEnd = searchTime + duration;
        const overlap = dayEvents.find((e) => potentialStart < e.end && potentialEnd > e.start);
        if (overlap) {
            searchTime = overlap.start - duration;
        } else {
            foundStart = searchTime;
            break;
        }
    }

    if (foundStart !== -1) {
        return { position: { start: foundStart, end: foundStart + duration } };
    }
    return null;
};

const resolveShiftDown = (ctx: ConflictContext): ConflictResolutionResult | null => {
    const { event, allEvents } = ctx;
    const duration = event.end - event.start;

    const sorted = [...allEvents].filter((e) => e.uiId !== event.uiId).sort((a, b) => a.start - b.start);

    const dayStart = new Date(event.start).setHours(0, 0, 0, 0);
    const dayEnd = new Date(event.start).setHours(23, 59, 59, 999);
    const dayEvents = sorted.filter((e) => e.end > dayStart && e.start < dayEnd);

    let searchTime = event.start;
    let foundStart = -1;

    while (searchTime < dayEnd) {
        const overlap = dayEvents.find((e) => searchTime < e.end && searchTime + duration > e.start);
        if (overlap) {
            searchTime = overlap.end;
        } else {
            foundStart = searchTime;
            break;
        }
    }

    if (foundStart !== -1) {
        return { position: { start: foundStart, end: foundStart + duration } };
    }
    return null;
};

const resolveTruncate = (ctx: ConflictContext): ConflictResolutionResult | null => {
    const { event, overlaps } = ctx;

    let allowedStart = event.start;
    let allowedEnd = event.end;

    const isFullyContained = overlaps.some((ov) => ov.start <= event.start && ov.end >= event.end);
    if (isFullyContained) return null;

    overlaps.forEach((ov) => {
        if (ov.start > event.start && ov.start < event.end) {
            allowedEnd = Math.min(allowedEnd, ov.start);
        }
        if (ov.end > event.start && ov.end < event.end) {
            allowedStart = Math.max(allowedStart, ov.end);
        }
    });

    if (allowedEnd <= allowedStart) return null;

    return { position: { start: allowedStart, end: allowedEnd } };
};

const resolveForce = (ctx: ConflictContext): ConflictResolutionResult | null => {
    const { event, overlaps } = ctx;
    const mutations: EventMutation[] = [];

    for (const ov of overlaps) {
        if (event.start <= ov.start && event.end >= ov.end) {
            mutations.push({ action: "remove", event: ov });
            continue;
        }

        if (event.start > ov.start && event.start < ov.end) {
            mutations.push({ action: "update", event: ov, start: ov.start, end: event.start });
        }

        if (event.end > ov.start && event.end < ov.end) {
            mutations.push({ action: "update", event: ov, start: event.end, end: ov.end });
        }

        if (event.start > ov.start && event.end < ov.end) {
            const headSize = event.start - ov.start;
            const tailSize = ov.end - event.end;
            if (headSize > tailSize) {
                mutations.push({ action: "update", event: ov, start: ov.start, end: event.start });
            } else {
                mutations.push({ action: "update", event: ov, start: event.end, end: ov.end });
            }
        }
    }

    return { position: { start: event.start, end: event.end }, mutations };
};

const defaultStrategies: ConflictResolutionStrategy[] = [
    {
        id: "shift-up",
        label: "Move to Previous Slot",
        subtitle: "Shift up to next free gap",
        icon: mdiArrowUpThin,
        resolve: resolveShiftUp
    },
    {
        id: "shift-down",
        label: "Move to Next Slot",
        subtitle: "Shift down to next free gap",
        icon: mdiArrowDownThin,
        resolve: resolveShiftDown
    },
    {
        id: "truncate",
        label: "Fit to Gap",
        subtitle: "Truncate this event to fit",
        icon: mdiArrowCollapseVertical,
        resolve: resolveTruncate
    },
    {
        id: "force",
        label: "Force Position",
        subtitle: "Shrink or remove conflicting events",
        icon: mdiAlertBoxOutline,
        variant: "error",
        resolve: resolveForce
    }
];
</script>
