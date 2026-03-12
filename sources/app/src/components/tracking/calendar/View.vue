<template>
    <VCalendar
        @mousedown:event="beginMoveEvent"
        @mousedown:time="beginGridInteraction"
        @mouseleave="cancelInteractionOnLeave"
        @mousemove:time="updateInteractionFromPointer"
        @mouseup:time="finishInteraction"
        v-bind="$attrs"
        :end="end"
        :eventRipple="false"
        :events="events"
        :start="start"
        :weekdays="[1, 2, 3, 4, 5]"
        color="primary"
        eventOverlapMode="column"
        type="week"
    >
        <template #event="{ event }">
            <VHover v-if="isTimeEntryEvent(event)" v-slot="{ isHovering, props }">
                <div :id="event.uiId" v-bind="props" class="h-100 pa-2 text-truncate">
                    <template v-if="event.kind === 'existing'">
                        <VProgressLinear :indeterminate="isUpdating(event.timeEntry.id)" />
                    </template>
                    <VSheet v-show="isHovering && interaction.kind === 'idle'" class="position-absolute d-flex ga-2 rounded" style="top: 5px; right: 5px">
                        <VIconBtn
                            v-if="event.kind === 'existing' || event.kind === 'suggestion'"
                            @click.stop="remove.start(event)"
                            @mousedown.stop
                            :icon="mdiDelete"
                            iconColor="error"
                            variant="flat"
                        />
                        <VIconBtn
                            v-if="event.kind === 'suggestion'"
                            @click.stop="acceptSuggestion(event)"
                            @mousedown.stop
                            :icon="mdiCheck"
                            iconColor="success"
                            variant="flat"
                        />
                        <VIconBtn
                            v-if="event.kind === 'existing' || event.kind === 'suggestion'"
                            @click.stop="edit.start(event)"
                            @mousedown.stop
                            :icon="mdiPencil"
                            iconColor="white"
                            variant="flat"
                        />
                    </VSheet>
                    <p v-if="event.kind === 'existing' || event.kind === 'suggestion'">
                        {{ event.timeEntry.taskId }}
                    </p>
                    <p v-else>{{ $t("calendar.event.draft") }}</p>
                    <p>{{ dateFormatter.format(event.start, "fullTime24h") }} - {{ dateFormatter.format(event.end, "fullTime24h") }}</p>
                </div>
                <div @mousedown.stop="beginResizeEvent(event)" class="v-event-drag-bottom" />
            </VHover>
        </template>
    </VCalendar>
    <TrackingCalendarFeaturesCreateOverlay v-if="interaction.kind === 'create'" v-model:interaction="interaction" />
    <TrackingCalendarFeaturesEditOverlay v-if="interaction.kind === 'edit'" v-model:interaction="interaction" />
    <TrackingCalendarFeaturesConflictOverlay v-if="interaction.kind === 'conflict'" v-model:interaction="interaction" />
    <TrackingCalendarFeaturesDeleteOverlay v-if="interaction.kind === 'delete'" v-model:interaction="interaction" />
</template>

<script setup lang="ts">
import type { EventSlotScope } from "vuetify/lib/components/VCalendar/VCalendar.mjs";
import type { CalendarDayBodySlotScope, CalendarEvent } from "vuetify/lib/components/VCalendar/types.mjs";
import { isTimeEntryEvent, type SuggestionTimeEntryEvent, type TimeEntryEvent } from "./types";
import { useMove } from "./composables/useMove";
import { useResize } from "./composables/useResize";
import { useDraft } from "./composables/useDraft";
import { useCreate } from "./composables/useCreate";
import { useEdit } from "./composables/useEdit";
import { useDelete } from "./composables/useDelete";
import { useConflict } from "./composables/useConflict";

const calendarStore = useCalendarStore();
const timeEntryStore = useTimeEntryStore();

const { events, interaction } = storeToRefs(calendarStore);
const { isUpdating } = storeToRefs(timeEntryStore);

const move = useMove();
const resize = useResize();
const draft = useDraft();
const create = useCreate();
const edit = useEdit();
const remove = useDelete();
const conflict = useConflict();

const dateFormatter = useDate();

onBeforeUnmount(() => {
    move.cancel();
    resize.cancel();
    draft.cancel();
    create.cancel();
    edit.cancel();
    remove.cancel();
    conflict.cancel();
});

const acceptSuggestion = (event: SuggestionTimeEntryEvent) => {
    create.start(event);
};

const canStartInteraction = (currentKind: string): boolean => {
    return currentKind !== "create" && currentKind !== "edit" && currentKind !== "conflict" && currentKind !== "delete";
};

const canAdjustEvent = (event: CalendarEvent): boolean => {
    if (canStartInteraction(interaction.value.kind)) return true;
    if (interaction.value.kind !== "conflict") return false;

    return event.uiId === interaction.value.event.uiId;
};

const beginMoveEvent = (_nativeEvent: Event, { event, timed }: EventSlotScope) => {
    if (!event || !timed) return;
    if (!canAdjustEvent(event)) return;
    move.start(event as TimeEntryEvent);
};

const beginResizeEvent = (event: CalendarEvent) => {
    if (!canAdjustEvent(event)) return;
    resize.start(event as TimeEntryEvent);
};

const beginGridInteraction = (_nativeEvent: Event, tms: CalendarDayBodySlotScope) => {
    if (!canStartInteraction(interaction.value.kind)) return;

    const mouseMs = toTime(tms);

    if (interaction.value.kind === "move" && interaction.value.pointerOffsetMs === undefined) {
        move.setPointerOffset(mouseMs);
        return;
    }

    draft.start(mouseMs);
};

const updateInteractionFromPointer = (_nativeEvent: Event, tms: CalendarDayBodySlotScope) => {
    const mouseMs = toTime(tms);
    move.update(mouseMs);
    resize.update(mouseMs);
    draft.update(mouseMs);
};

const finishInteraction = async () => {
    draft.finish();
    await move.finish();
    await resize.finish();
};

const cancelInteractionOnLeave = () => {
    resize.cancel();
    move.cancel();
    draft.cancel();
};

const toTime = (tms: CalendarDayBodySlotScope) => {
    return new Date(tms.year, tms.month - 1, tms.day, tms.hour, tms.minute).getTime();
};

const start = computed<Date>(() => {
    const now = new Date();
    const daysSinceMonday = (now.getDay() + 6) % 7;
    const monday = new Date(now);
    monday.setDate(now.getDate() - daysSinceMonday);
    monday.setHours(0, 0, 0, 0);
    return monday;
});

const end = computed<Date>(() => {
    const monday = start.value;
    const friday = new Date(monday);
    friday.setDate(monday.getDate() + 4);
    friday.setHours(23, 59, 59, 999);
    return friday;
});
</script>

<style scoped>
:deep(.v-event-timed) {
    user-select: none;
}

.v-event-drag-bottom {
    position: absolute;
    left: 0;
    right: 0;
    bottom: 4px;
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
</style>
