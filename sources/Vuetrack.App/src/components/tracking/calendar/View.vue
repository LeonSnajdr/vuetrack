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
            <TrackingCalendarEvent
                v-if="isTimeEntryEvent(event)"
                @resize="(edge, nativeEvent) => beginResizeEvent(event, edge, nativeEvent)"
                :event="event"
            />
        </template>
    </VCalendar>
    <TrackingCalendarContextMenu />
    <TrackingCalendarEventDetails />
    <TrackingCalendarOverlays />
</template>

<script setup lang="ts">
import type { EventSlotScope } from "vuetify/lib/components/VCalendar/VCalendar.mjs";
import type { CalendarDayBodySlotScope, CalendarEvent } from "vuetify/lib/components/VCalendar/types.mjs";
import { isTimeEntryEvent, type EventEdge } from "./types";
import { useMove } from "./composables/useMove";
import { useResize } from "./composables/useResize";
import { useDraft } from "./composables/useDraft";
import { useCreate } from "./composables/useCreate";
import { useEdit } from "./composables/useEdit";
import { useDelete } from "./composables/useDelete";
import { useConflict } from "./composables/useConflict";
import { useGestureArm } from "./composables/useGestureArm";
import { useCalendarTimePeriod } from "./composables/useCalendarTimePeriod";
import { useCalendarInterval } from "./composables/useCalendarInterval";
import { useEventShortcuts } from "./composables/useEventShortcuts";
import { useEventContextMenu } from "./composables/useEventContextMenu";
import { useEventPolicy } from "./composables/useEventPolicy";
import { useEventSelection } from "./composables/useEventSelection";

const calendarStore = useCalendarStore();

const { events, gesture, isLoadingEvents } = storeToRefs(calendarStore);

const move = useMove();
const resize = useResize();
const draft = useDraft();
const create = useCreate();
const edit = useEdit();
const remove = useDelete();
const conflict = useConflict();
const { select, clearSelection } = useEventSelection();
const { isArmed, armGesture, setAnchorTime, clearArm, promoteArm } = useGestureArm();
const { jumpToDay } = useTrackingTimePeriod();
const { start, end, weekdays, isReadonly, calendarType } = useCalendarTimePeriod();
const { intervalMinutes, intervalCount, firstInterval } = useCalendarInterval();

const contextMenu = useEventContextMenu();
const policy = useEventPolicy();

useEventShortcuts();

// A shortcut must never hit an event that scrolled out of the shown range.
watch([start, end], () => clearSelection());

onBeforeUnmount(() => {
    cancelAll();
});

const cancelAll = () => {
    clearArm();
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

const canAdjustEvent = (event: CalendarEvent): boolean => {
    if (!isTimeEntryEvent(event)) return false;
    return policy.canStartGesture(event);
};

const isLeftClick = (nativeEvent: Event): boolean => {
    return (nativeEvent as MouseEvent).button === 0;
};

const beginMoveEvent = (nativeEvent: Event, { event, timed }: EventSlotScope) => {
    if (isReadonly.value) return;
    if (!isLeftClick(nativeEvent)) return;
    if (!event || !timed) return;
    if (!isTimeEntryEvent(event)) return;

    // Selecting is always allowed: it is what the shortcuts and the conflict
    // resolutions read, even where a gesture is not.
    select(event);
    if (!canAdjustEvent(event)) return;

    armGesture({ kind: "move", event }, nativeEvent as MouseEvent);
};

const openContextMenu = (nativeEvent: Event, { event }: EventSlotScope) => {
    contextMenu.open(nativeEvent, event);
};

const beginResizeEvent = (event: CalendarEvent, edge: EventEdge, nativeEvent: MouseEvent) => {
    if (isReadonly.value) return;
    if (!isTimeEntryEvent(event)) return;

    select(event);
    if (!canAdjustEvent(event)) return;

    armGesture({ kind: "resize", event, edge }, nativeEvent);
};

const beginGridInteraction = (nativeEvent: Event, tms: CalendarDayBodySlotScope) => {
    if (isReadonly.value) return;
    if (!isLeftClick(nativeEvent)) return;

    const mouseMs = toTime(tms);

    // The press already belongs to an event, so it never drafts a new one.
    if (isArmed.value) {
        setAnchorTime(mouseMs);
        return;
    }

    if (gesture.value.kind !== "idle") return;
    if (!policy.canOpenTask()) return;

    clearSelection();
    draft.start(mouseMs);
};

const updateInteractionFromPointer = (nativeEvent: Event, tms: CalendarDayBodySlotScope) => {
    if (isReadonly.value) return;

    const mouseMs = toTime(tms);

    if (isArmed.value) {
        const promoted = promoteArm(nativeEvent as MouseEvent, mouseMs);
        if (!promoted) return;
    }

    move.update(mouseMs);
    resize.update(mouseMs);
    draft.update(mouseMs);
};

const finishInteraction = async () => {
    if (isReadonly.value) return;

    clearArm();

    draft.finish();
    await move.finish();
    await resize.finish();
};

const cancelInteractionOnLeave = () => {
    if (isReadonly.value) return;

    clearArm();

    resize.cancel();
    move.cancel();
    draft.cancel();
};

const toTime = (tms: CalendarDayBodySlotScope) => {
    return new Date(tms.year, tms.month - 1, tms.day, tms.hour, tms.minute).getTime();
};
</script>

<style scoped>
:deep(.v-event-timed),
:deep(.v-event) {
    user-select: none;
    min-height: 28px;
    background-color: transparent !important;
    border: none;
    box-shadow: none;
    border-radius: 6px;
}
</style>
