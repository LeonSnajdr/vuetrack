<template>
    <div :id="event.uiId" class="h-100">
        <div class="h-100 pa-1 d-flex flex-column ga-2 text-truncate">
            <div class="d-flex flex-col justify-space-between">
                <div class="text-truncate">
                    <template v-if="event.kind === 'existing' || event.kind === 'suggestion'">
                        {{ event.timeEntry.taskId ?? event.timeEntry.project.name }}
                    </template>
                    <template v-else>{{ $t("calendar.event.draft") }}</template>
                </div>
                <VChip class="text-label-small flex-shrink-0" color="" density="compact" variant="tonal">
                    {{ dateFormatter.format(event.start, "fullTime24h") }} - {{ dateFormatter.format(event.end, "fullTime24h") }}
                </VChip>
            </div>
            <div v-if="event.kind === 'existing' || event.kind === 'suggestion'" class="text-medium-emphasis text-truncate">
                {{ event.timeEntry.comment }}
            </div>
        </div>
    </div>
    <!-- TODO Might be allowed during conflict-->
    <div v-if="canResize" @mousedown.stop="emit('resize', 'start')" class="v-event-drag-top" />
    <div v-if="canResize" @mousedown.stop="emit('resize', 'end')" class="v-event-drag-bottom" />
</template>

<script setup lang="ts">
import type { EventEdge, TimeEntryEvent } from "./types";
import { useCalendarTimePeriod } from "./composables/useCalendarTimePeriod";

const emit = defineEmits<{
    resize: [edge: EventEdge];
}>();

defineProps<{
    event: TimeEntryEvent;
}>();

const calendarStore = useCalendarStore();
const { interaction } = storeToRefs(calendarStore);
const { isReadonly } = useCalendarTimePeriod();

const dateFormatter = useDate();

const canResize = computed(() => !isReadonly.value && interaction.value.kind === "idle");
</script>

<style scoped>
.v-event-drag-top,
.v-event-drag-bottom {
    position: absolute;
    left: 0;
    right: 0;
    height: 4px;
    cursor: ns-resize;

    &::after {
        display: none;
        position: absolute;
        left: 50%;
        height: 4px;
        border-top: 1px solid white;
        border-bottom: 1px solid white;
        width: 16px;
        margin-left: -8px;
        opacity: 0.8;
        content: "";
    }

    &:hover::after {
        display: block;
    }
}

.v-event-drag-top {
    top: 4px;
}

.v-event-drag-bottom {
    bottom: 4px;
}
</style>
