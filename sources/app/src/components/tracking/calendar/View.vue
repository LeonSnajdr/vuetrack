<template>
    <Teleport to="#tracking-toolbar-prepend" defer>
        <TrackingCalendarIntervalSelection />
    </Teleport>
    <VCalendar
        @click:date="jumpToDate"
        @click:more="jumpToMoreDay"
        @mousedown:event="beginMoveEvent"
        @mousedown:time="beginGridInteraction"
        @mouseleave="cancelInteractionOnLeave"
        @mousemove:time="updateInteractionFromPointer"
        @mouseup:time="finishInteraction"
        v-bind="$attrs"
        :end="end"
        :eventRipple="false"
        :events="events"
        :intervalCount="intervalCount"
        :intervalMinutes="intervalMinutes"
        :start="start"
        :type="calendarType"
        :weekdays="weekdays"
        color="primary"
        eventOverlapMode="column"
    >
        <template #day-body="day">
            <TrackingCalendarCurrentTimeLine :day="day" />
        </template>
        <template #event="{ event }">
            <template v-if="isTimeEntryEvent(event)">
                <div :id="event.uiId" class="h-100 position-relative">
                    <div class="h-100 pa-1 d-flex flex-column ga-2 text-truncate">
                        <div class="d-flex flex-col justify-space-between">
                            <div class="text-truncate">
                                <template v-if="event.kind === 'existing' || event.kind === 'suggestion'">
                                    {{ event.timeEntry.taskId }}
                                </template>
                                <template v-else>{{ $t("calendar.event.draft") }}</template>
                            </div>
                            <VChip class="text-label-small flex-shrink-0" color="" density="compact" variant="tonal">
                                {{ dateFormatter.format(event.start, "fullTime24h") }} - {{ dateFormatter.format(event.end, "fullTime24h") }}
                                <VIcon :icon="mdiClockEditOutline" class="ml-1" />
                                <VMenu :closeDelay="0" :disabled="!(interaction.kind === 'idle')" :openDelay="0" activator="parent" location="end" openOnHover>
                                    <VSheet class="d-flex ga-2 pa-1">
                                        <VSpacer />
                                        <VIconBtn
                                            v-if="event.kind === 'suggestion'"
                                            @click="acceptSuggestion(event)"
                                            :icon="mdiCheck"
                                            iconColor="success"
                                            variant="flat"
                                        />
                                        <VIconBtn
                                            v-if="event.kind === 'existing' || event.kind === 'suggestion'"
                                            @click="edit.start(event)"
                                            :icon="mdiPencil"
                                            iconColor="white"
                                            variant="flat"
                                        />
                                        <VIconBtn
                                            v-if="event.kind === 'existing' || event.kind === 'suggestion'"
                                            @click="remove.start(event)"
                                            :icon="mdiDelete"
                                            iconColor="error"
                                            variant="flat"
                                        />
                                    </VSheet>
                                </VMenu>
                            </VChip>
                        </div>
                    </div>
                </div>
                <div v-if="!isReadonly" @mousedown.stop="beginResizeEvent(event)" class="v-event-drag-bottom" />
            </template>
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
import { useCalendarTimePeriod } from "./composables/useCalendarTimePeriod";
import { useCalendarInterval } from "./composables/useCalendarInterval";

const calendarStore = useCalendarStore();

const { events, interaction } = storeToRefs(calendarStore);

const move = useMove();
const resize = useResize();
const draft = useDraft();
const create = useCreate();
const edit = useEdit();
const remove = useDelete();
const conflict = useConflict();
const { jumpToDay } = useTrackingTimePeriod();
const { start, end, weekdays, isReadonly, calendarType } = useCalendarTimePeriod();
const { intervalMinutes, intervalCount } = useCalendarInterval();

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

const jumpToDate = (_nativeEvent: Event, day: { year: number; month: number; day: number }) => {
    jumpToDay(new Date(day.year, day.month - 1, day.day));
};

const jumpToMoreDay = (_nativeEvent: Event, day: { year: number; month: number; day: number }) => {
    jumpToDay(new Date(day.year, day.month - 1, day.day));
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
    if (isReadonly.value) return;
    if (!event || !timed) return;
    if (!canAdjustEvent(event)) return;
    move.start(event as TimeEntryEvent);
};

const beginResizeEvent = (event: CalendarEvent) => {
    if (isReadonly.value) return;
    if (!canAdjustEvent(event)) return;
    resize.start(event as TimeEntryEvent);
};

const beginGridInteraction = (_nativeEvent: Event, tms: CalendarDayBodySlotScope) => {
    if (isReadonly.value) return;
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
