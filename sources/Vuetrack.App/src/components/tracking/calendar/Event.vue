<template>
    <div
        :id="event.uiId"
        @mouseenter="details.open($event, event)"
        @mouseleave="details.close()"
        @mousemove="details.move($event, event)"
        :class="['h-100', 'tc-event', `tc-${event.kind}`]"
    >
        <div class="h-100 py-1 px-2 d-flex flex-column ga-1 text-truncate">
            <div class="tc-header">
                <div class="tc-title text-on-surface text-truncate font-weight-medium text-high-emphasis">
                    <template v-if="event.kind === 'existing'">{{ event.timeEntry.taskId ?? event.timeEntry.project.name }}</template>
                    <template v-else-if="event.kind === 'suggestion'">{{ event.timeEntry.taskId ?? event.timeEntry.projectName }}</template>
                    <template v-else>{{ $t("calendar.event.draft") }}</template>
                </div>
                <div class="tc-time text-label-small text-medium-emphasis text-truncate">
                    {{ dateFormatter.format(event.start, "fullTime24h") }} - {{ dateFormatter.format(event.end, "fullTime24h") }}
                </div>
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
import { useEventDetails } from "./composables/useEventDetails";

const emit = defineEmits<{
    resize: [edge: EventEdge];
}>();

defineProps<{
    event: TimeEntryEvent;
}>();

const calendarStore = useCalendarStore();
const { interaction } = storeToRefs(calendarStore);
const { isReadonly } = useCalendarTimePeriod();
const details = useEventDetails();

const dateFormatter = useDate();

const canResize = computed(() => !isReadonly.value && interaction.value.kind === "idle");
</script>

<style scoped>
.tc-event {
    border-radius: 6px;
    border: 1px solid;
    border-left-width: 3px;
    overflow: hidden;
    margin-right: 2px;
    container-type: inline-size;
}

.tc-header {
    display: flex;
    flex-direction: column;
    gap: 2px;
    min-width: 0;
}

.tc-title {
    min-width: 0;
}

@container (min-width: 150px) {
    .tc-header {
        flex-direction: row;
        align-items: flex-start;
        gap: 8px;
    }

    .tc-title {
        flex: 1 1 auto;
    }

    .tc-time {
        margin-left: auto;
        flex-shrink: 0;
    }
}

.tc-existing {
    background-color: color-mix(in srgb, rgb(var(--v-theme-primary)) 22%, rgb(var(--v-theme-surface)));
    border-color: color-mix(in srgb, rgb(var(--v-theme-primary)) 45%, rgb(var(--v-theme-surface)));
    border-left-color: rgb(var(--v-theme-primary));
}

.tc-suggestion {
    background-color: color-mix(in srgb, rgb(var(--v-theme-tertiary)) 22%, rgb(var(--v-theme-surface)));
    border-color: color-mix(in srgb, rgb(var(--v-theme-tertiary)) 45%, rgb(var(--v-theme-surface)));
    border-left-color: rgb(var(--v-theme-tertiary));
}

.tc-draft {
    background-color: color-mix(in srgb, rgb(var(--v-theme-secondary)) 22%, rgb(var(--v-theme-surface)));
    border-color: color-mix(in srgb, rgb(var(--v-theme-secondary)) 45%, rgb(var(--v-theme-surface)));
    border-left-color: rgb(var(--v-theme-secondary));
}

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
        border-top: 1px solid rgb(var(--v-theme-on-surface));
        border-bottom: 1px solid rgb(var(--v-theme-on-surface));
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
