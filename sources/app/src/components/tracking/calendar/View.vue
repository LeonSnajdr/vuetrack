<template>
    <VCalendar
        @mousedown:event="beginMoveEvent"
        @mousedown:time="beginGridInteraction"
        @mouseleave="cancelInteractionOnLeave"
        @mousemove:time="updateInteractionFromPointer"
        @mouseup:time="finishInteraction"
        :end="end"
        :eventRipple="false"
        :events="events"
        :start="start"
        :weekdays="[1, 2, 3, 4, 5]"
        class="h-100"
        color="primary"
        type="custom-daily"
    >
        <template #event="{ event, timed, eventSummary }">
            <div :id="(event as TimeEntryEvent).uiId" class="v-event-draggable">
                <component :is="eventSummary" />
            </div>

            <div v-if="timed" @mousedown.stop="beginResizeEvent(event)" class="v-event-drag-bottom" />
        </template>
    </VCalendar>

    <template v-if="interaction.kind === 'create'">
        <VMenu
            @update:modelValue="onNameMenuToggle"
            :closeOnContentClick="false"
            :modelValue="interaction.kind === 'create'"
            :target="'#' + interaction.event.uiId"
            location="right"
        >
            <VCard class="pa-3" width="320">
                <VCardTitle class="text-subtitle-1 pa-0">Name the event</VCardTitle>

                <VTextField
                    v-model.trim="interaction.event.taskId"
                    @keydown.enter.prevent="confirmEvent"
                    @keydown.esc.prevent="cancelDraft"
                    class="mt-3"
                    density="compact"
                    label="Taskid"
                />

                <div class="d-flex justify-end ga-2 mt-2">
                    <VBtn @click="cancelDraft" variant="text">Cancel</VBtn>
                    <VBtn @click="confirmEvent" color="primary">Save</VBtn>
                </div>
            </VCard>
        </VMenu>
    </template>
</template>

<script setup lang="ts">
import type { CalendarDayBodySlotScope, CalendarEvent } from "vuetify/lib/components/VCalendar/types.mjs";
import type { EventSlotScope } from "vuetify/lib/components/VCalendar/VCalendar.mjs";

type BaseCalendarEvent = CalendarEvent & {
    uiId: string;
};

type TimeEntryEvent = ({ kind: "draft" } & BaseCalendarEvent & TimeEntryCreateContract) | ({ kind: "existing" } & BaseCalendarEvent & TimeEntryContract);

type Interaction =
    | { kind: "idle" }
    | { kind: "move"; event: TimeEntryEvent; pointerOffsetMs?: number }
    | { kind: "resize"; event: TimeEntryEvent; originalEndMs: number }
    | { kind: "draft"; event: TimeEntryEvent; anchorStartMs: number }
    | { kind: "create"; event: TimeEntryEvent };

const events = ref<TimeEntryEvent[]>([]);
const interaction = ref<Interaction>({ kind: "idle" });

const onTimeEntryCreate = async (createContract: TimeEntryCreateContract): Promise<TimeEntryContract> => {
    const newTimeEntry: TimeEntryContract = {
        id: "testId" as TimeEntryId,
        user: "testUser",
        ...createContract
    };

    return newTimeEntry;
};

const removeEvent = (ev?: TimeEntryEvent) => {
    if (!ev) return;
    const i = events.value.indexOf(ev);
    if (i !== -1) events.value.splice(i, 1);
};

const confirmEvent = async () => {
    if (interaction.value.kind !== "create") return;

    if (interaction.value.event.kind !== "draft" || !interaction.value.event.taskId) {
        cancelDraft();
        return;
    }

    const createdTimeEntry = await onTimeEntryCreate(interaction.value.event);

    Object.assign(interaction.value.event, createdTimeEntry, {
        kind: "existing"
    });

    interaction.value = { kind: "idle" };
};

const cancelDraft = () => {
    if (interaction.value.kind !== "create") {
        interaction.value = { kind: "idle" };
        return;
    }

    removeEvent(interaction.value.event);
    interaction.value = { kind: "idle" };
};

const onNameMenuToggle = (open: boolean) => {
    if (!open && interaction.value.kind === "create") cancelDraft();
};

const beginMoveEvent = (_nativeEvent: Event, { event, timed }: EventSlotScope) => {
    if (interaction.value.kind === "create") return;
    if (!event || !timed) return;

    const ev = event as TimeEntryEvent;
    if (ev.kind === "draft") return;

    interaction.value = { kind: "move", event: ev, pointerOffsetMs: undefined };
};

const beginResizeEvent = (event: CalendarEvent) => {
    if (interaction.value.kind === "create") return;

    const ev = event as TimeEntryEvent;
    if (ev.kind === "draft") return;

    interaction.value = { kind: "resize", event: ev, originalEndMs: ev.end };
};

const beginGridInteraction = (_nativeEvent: Event, tms: CalendarDayBodySlotScope) => {
    if (interaction.value.kind === "create") return;

    const mouseMs = toTime(tms);

    if (interaction.value.kind === "move" && interaction.value.pointerOffsetMs === undefined) {
        interaction.value.pointerOffsetMs = mouseMs - interaction.value.event.start;
        return;
    }

    const anchorStartMs = roundTime(mouseMs);

    const newEvent: TimeEntryEvent = {
        kind: "draft",
        taskId: "",
        color: "#673AB7",
        start: anchorStartMs,
        end: anchorStartMs,
        timed: true,
        uiId: `event-uiId-${uuidv4()}`
    };

    events.value.push(newEvent);
    interaction.value = { kind: "draft", event: newEvent, anchorStartMs };
};

const updateInteractionFromPointer = (_nativeEvent: Event, tms: CalendarDayBodySlotScope) => {
    if (interaction.value.kind === "create") return;

    const mouseMs = toTime(tms);

    switch (interaction.value.kind) {
        case "move": {
            const { event, pointerOffsetMs } = interaction.value;
            if (pointerOffsetMs === undefined) return;

            const duration = event.end - event.start;
            const newStart = roundTime(mouseMs - pointerOffsetMs);

            event.start = newStart;
            event.end = newStart + duration;
            return;
        }
        case "resize": {
            const { event } = interaction.value;
            const mouseRounded = roundTime(mouseMs, false);
            event.end = Math.max(mouseRounded, event.start);
            return;
        }
        case "draft": {
            const { event, anchorStartMs } = interaction.value;
            const mouseRounded = roundTime(mouseMs, false);
            event.start = Math.min(mouseRounded, anchorStartMs);
            event.end = Math.max(mouseRounded, anchorStartMs);
            return;
        }
        default:
            return;
    }
};

const finishInteraction = () => {
    if (interaction.value.kind === "create") return;

    if (interaction.value.kind === "draft") {
        interaction.value = { kind: "create", event: interaction.value.event };
    } else {
        interaction.value = { kind: "idle" };
    }
};

const cancelInteractionOnLeave = () => {
    if (interaction.value.kind === "create") return;

    if (interaction.value.kind === "resize") {
        interaction.value.event.end = interaction.value.originalEndMs;
    }

    if (interaction.value.kind === "draft") {
        removeEvent(interaction.value.event);
    }

    interaction.value = { kind: "idle" };
};

const roundTime = (time: number, down = true) => {
    const roundTo = 15;
    const step = roundTo * 60 * 1000;
    return down ? time - (time % step) : time + (step - (time % step));
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
.v-event-draggable {
    padding-left: 6px;
}

.v-event-timed {
    user-select: none;
    -webkit-user-select: none;
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
