<template>
    <Teleport to="#tracking-toolbar-prepend" defer>
        <TrackingCalendarIntervalSelection />
    </Teleport>
    <VProgressLinear :indeterminate="isLoadingEvents" style="margin-bottom: -2px" />
    <VCalendar
        @click:date="jumpToDate"
        @click:more="jumpToMoreDay"
        @contextmenu:event="openContextMenu"
        @mousedown:event="beginMoveEvent"
        @mousedown:time="beginGridInteraction"
        @mouseleave="cancelInteractionOnLeave"
        @mousemove:time="updateInteractionFromPointer"
        @mouseup:time="finishInteraction"
        v-bind="$attrs"
        :end="end"
        :eventRipple="false"
        :events="events"
        :firstInterval="firstInterval"
        :intervalCount="intervalCount"
        :intervalMinutes="intervalMinutes"
        :start="start"
        :type="calendarType"
        :weekdays="weekdays"
        class="border-s-0"
        color="primary"
        eventOverlapMode="column"
    >
        <template #day-body="day">
            <TrackingCalendarCurrentTimeLine :day="day" />
        </template>
        <template #event="{ event }">
            <TrackingCalendarEvent v-if="isTimeEntryEvent(event)" @resize="beginResizeEvent(event, $event)" :event="event" />
        </template>
    </VCalendar>
    <TrackingCalendarContextMenu ref="contextMenuRef" />
    <TrackingCalendarOverlays />
</template>

<script setup lang="ts">
import type { EventSlotScope } from "vuetify/lib/components/VCalendar/VCalendar.mjs";
import type { CalendarDayBodySlotScope, CalendarEvent } from "vuetify/lib/components/VCalendar/types.mjs";
import { isTimeEntryEvent, type EventEdge, type InteractionKind, type TimeEntryEvent } from "./types";
import { useMove } from "./composables/useMove";
import { useResize } from "./composables/useResize";
import { useDraft } from "./composables/useDraft";
import { useCreate } from "./composables/useCreate";
import { useEdit } from "./composables/useEdit";
import { useDelete } from "./composables/useDelete";
import { useConflict } from "./composables/useConflict";
import { useCalendarTimePeriod } from "./composables/useCalendarTimePeriod";
import { useCalendarInterval } from "./composables/useCalendarInterval";

const calendarStore = useCalendarStore();

const { events, interaction, isLoadingEvents } = storeToRefs(calendarStore);

const move = useMove();
const resize = useResize();
const draft = useDraft();
const create = useCreate();
const edit = useEdit();
const remove = useDelete();
const conflict = useConflict();
const { jumpToDay } = useTrackingTimePeriod();
const { start, end, weekdays, isReadonly, calendarType } = useCalendarTimePeriod();
const { intervalMinutes, intervalCount, firstInterval } = useCalendarInterval();

const contextMenuRef = useTemplateRef("contextMenuRef");

onBeforeUnmount(() => {
    cancelAll();
});

const cancelAll = () => {
    move.cancel();
    resize.cancel();
    draft.cancel();
    create.cancel();
    edit.cancel();
    remove.cancel();
    conflict.cancel();
};

const jumpToDate = (_nativeEvent: Event, day: { year: number; month: number; day: number }) => {
    jumpToDay(new Date(day.year, day.month - 1, day.day));
};

const jumpToMoreDay = (_nativeEvent: Event, day: { year: number; month: number; day: number }) => {
    jumpToDay(new Date(day.year, day.month - 1, day.day));
};

const canStartInteraction = (currentKind: InteractionKind): boolean => {
    return currentKind !== "create" && currentKind !== "edit" && currentKind !== "conflict" && currentKind !== "delete";
};

const canAdjustEvent = (event: CalendarEvent): boolean => {
    if (canStartInteraction(interaction.value.kind)) return true;
    if (interaction.value.kind !== "conflict") return false;

    return event.uiId === interaction.value.event.uiId;
};

const isLeftClick = (nativeEvent: Event): boolean => {
    return (nativeEvent as MouseEvent).button === 0;
};

const beginMoveEvent = (nativeEvent: Event, { event, timed }: EventSlotScope) => {
    if (isReadonly.value) return;
    if (!isLeftClick(nativeEvent)) return;
    if (!event || !timed) return;
    if (!canAdjustEvent(event)) return;
    move.start(event as TimeEntryEvent);
};

const openContextMenu = (nativeEvent: Event, { event }: EventSlotScope) => {
    contextMenuRef.value?.open(nativeEvent, event);
};

const beginResizeEvent = (event: CalendarEvent, edge: EventEdge) => {
    if (isReadonly.value) return;
    if (!canAdjustEvent(event)) return;
    resize.start(event as TimeEntryEvent, edge);
};

const beginGridInteraction = (nativeEvent: Event, tms: CalendarDayBodySlotScope) => {
    if (isReadonly.value) return;
    if (!isLeftClick(nativeEvent)) return;
    if (!canStartInteraction(interaction.value.kind)) return;

    const mouseMs = toTime(tms);

    if (interaction.value.kind === "move" && interaction.value.pointerOffsetMs === undefined) {
        move.setPointerOffset(mouseMs);
        return;
    }

    draft.start(mouseMs);
};

const updateInteractionFromPointer = (_nativeEvent: Event, tms: CalendarDayBodySlotScope) => {
    if (isReadonly.value) return;
    const mouseMs = toTime(tms);
    move.update(mouseMs);
    resize.update(mouseMs);
    draft.update(mouseMs);
};

const finishInteraction = async () => {
    if (isReadonly.value) return;
    draft.finish();
    await move.finish();
    await resize.finish();
};

const cancelInteractionOnLeave = () => {
    if (isReadonly.value) return;
    resize.cancel();
    move.cancel();
    draft.cancel();
};

const toTime = (tms: CalendarDayBodySlotScope) => {
    return new Date(tms.year, tms.month - 1, tms.day, tms.hour, tms.minute).getTime();
};
</script>

<style scoped>
:deep(.v-event-timed) {
    user-select: none;
    min-height: 30px;
}

:deep(.v-event) {
    user-select: none;
    min-height: 30px;
}
</style>
