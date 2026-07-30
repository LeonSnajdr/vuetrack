<template>
    <div
        :id="event.uiId"
        @mouseenter="onMouseEnter"
        @mouseleave="onMouseLeave"
        @mousemove="details.move($event, event)"
        :class="[
            'h-100',
            'rounded',
            'border',
            'border-s-lg',
            'overflow-hidden',
            'tc-event',
            `tc-${event.kind}`,
            { 'tc-conflicting': isConflicting, 'tc-selected': isSelected },
            { 'tc-removed cursor-default opacity-40': isRemoved }
        ]"
    >
        <div class="h-100 py-1 px-2 d-flex flex-column ga-1 text-truncate">
            <div class="tc-header d-flex flex-wrap align-baseline">
                <div
                    :class="[
                        'flex-grow-1',
                        'text-on-surface',
                        'text-truncate',
                        'font-weight-medium',
                        'text-high-emphasis',
                        { 'text-decoration-line-through': isRemoved }
                    ]"
                >
                    <template v-if="event.kind === 'existing'">{{ event.timeEntry.taskId ?? event.timeEntry.project.name }}</template>
                    <template v-else-if="event.kind === 'suggestion'">{{ event.timeEntry.taskId ?? event.timeEntry.projectName }}</template>
                    <template v-else-if="event.kind === 'draft'">{{ event.createEntry.taskId ?? $t("calendar.event.draft") }}</template>
                </div>
                <div v-if="status" class="tc-status d-flex align-center align-self-start flex-shrink-0">
                    <VProgressCircular v-if="status === 'saving'" color="warning" size="12" width="2" indeterminate />
                    <VIcon v-else-if="status === 'removed'" :icon="mdiDelete" color="error" size="x-small" />
                    <VIcon v-else :icon="mdiContentSaveAlertOutline" color="warning" size="x-small" />
                </div>
                <div class="tc-time flex-0-0-100 ml-auto text-label-small text-medium-emphasis text-truncate">
                    {{ dateFormatter.format(event.start, "fullTime24h") }} - {{ dateFormatter.format(event.end, "fullTime24h") }}
                </div>
            </div>
            <div class="text-medium-emphasis text-truncate">
                <template v-if="event.kind === 'existing' || event.kind === 'suggestion'">
                    {{ event.timeEntry.comment }}
                </template>
                <template v-else-if="event.kind === 'draft'">
                    {{ event.createEntry.comment }}
                </template>
            </div>
        </div>
    </div>
    <div v-if="canResize" @mousedown.stop="emit('resize', 'start', $event)" class="v-event-drag-top" />
    <div v-if="canResize" @mousedown.stop="emit('resize', 'end', $event)" class="v-event-drag-bottom" />
</template>

<script setup lang="ts">
import type { EventEdge, TimeEntryEvent } from "./types";
import { useCalendarTimePeriod } from "./composables/useCalendarTimePeriod";
import { useChangeSet } from "./composables/useChangeSet";
import { useConflictDetection } from "./composables/useConflictDetection";
import { useEventDetails } from "./composables/useEventDetails";
import { useEventPolicy } from "./composables/useEventPolicy";
import { useEventSelection } from "./composables/useEventSelection";

const emit = defineEmits<{
    resize: [edge: EventEdge, nativeEvent: MouseEvent];
}>();

const props = defineProps<{
    event: TimeEntryEvent;
}>();

const calendarStore = useCalendarStore();
const { gesture } = storeToRefs(calendarStore);
const { isReadonly } = useCalendarTimePeriod();
const details = useEventDetails();
const policy = useEventPolicy();
const changeSet = useChangeSet();
const { conflictingUiIds } = useConflictDetection();
const { selectedUiId } = useEventSelection();

const dateFormatter = useDate();

const canResize = computed(() => {
    if (isReadonly.value) return false;
    if (gesture.value.kind !== "idle") return false;
    return policy.canStartGesture(props.event);
});

const isRemoved = computed(() => changeSet.isRemoved(props.event.uiId));

const isSaving = computed(() => changeSet.isSaving(props.event.uiId));

const isUnsaved = computed(() => changeSet.has(props.event.uiId));

const status = computed(() => {
    if (isSaving.value) return "saving";
    if (isRemoved.value) return "removed";
    if (isUnsaved.value) return "unsaved";
    return null;
});

const isConflicting = computed(() => conflictingUiIds.value.has(props.event.uiId));

const isSelected = computed(() => selectedUiId.value === props.event.uiId);

const onMouseEnter = (nativeEvent: MouseEvent) => {
    details.open(nativeEvent, props.event);
};

const onMouseLeave = () => {
    details.close();
};
</script>

<style scoped>
.tc-event {
    container-type: inline-size;
    background-color: color-mix(in srgb, rgb(var(--tc-accent)) 22%, rgb(var(--v-theme-surface)));
    border-color: color-mix(in srgb, rgb(var(--tc-accent)) 45%, rgb(var(--v-theme-surface)));
    border-inline-start-color: rgb(var(--tc-accent));
}

.tc-status {
    height: 1lh;
}

.tc-header {
    gap: 2px;
    min-width: 0;
}

@container (min-width: 150px) {
    .tc-header {
        gap: 8px;
    }

    .tc-time {
        flex-basis: auto;
    }
}

.tc-existing {
    --tc-accent: var(--v-theme-primary);
}

.tc-suggestion {
    --tc-accent: var(--v-theme-tertiary);
}

.tc-draft {
    --tc-accent: var(--v-theme-secondary);
}

.tc-conflicting {
    --tc-accent: var(--v-theme-error);
}

.tc-removed {
    --tc-accent: var(--v-theme-error);
    background-color: color-mix(in srgb, rgb(var(--tc-accent)) 10%, rgb(var(--v-theme-surface)));
    border-color: color-mix(in srgb, rgb(var(--tc-accent)) 35%, rgb(var(--v-theme-surface)));
    border-left-color: rgb(var(--tc-accent));
}

.tc-selected {
    background-color: color-mix(in srgb, rgb(var(--tc-accent)) 40%, rgb(var(--v-theme-surface)));
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
