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
        eventOverlapMode="column"
        type="custom-daily"
    >
        <template #event="{ event, timed }">
            <div :id="(event as TimeEntryEvent).uiId" class="v-event-draggable">
                <p>{{ (event as TimeEntryEvent).start }}</p>
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
                    v-model.trim="interaction.event.createEntry.taskId"
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

type BaseCalendarEvent = {
    uiId: string;
    timed: boolean;
    color: string;
    start: number;
    end: number;
};

type TimeEntryEvent =
    | ({ kind: "draft"; createEntry: TimeEntryCreateContract } & BaseCalendarEvent)
    | ({ kind: "existing"; timeEntry: TimeEntryContract } & BaseCalendarEvent)
    | ({ kind: "suggestion"; timeEntry: TimeEntrySuggestionContract } & BaseCalendarEvent);

type Interaction =
    | { kind: "idle" }
    | { kind: "move"; event: Extract<TimeEntryEvent, { kind: "existing" | "suggestion" }>; pointerOffsetMs?: number }
    | { kind: "resize"; event: Extract<TimeEntryEvent, { kind: "existing" | "suggestion" }>; originalEndMs: number }
    | { kind: "draft"; event: Extract<TimeEntryEvent, { kind: "draft" }>; anchorStartMs: number }
    | { kind: "create"; event: Extract<TimeEntryEvent, { kind: "draft" }> }
    | { kind: "conflict"; event: Extract<TimeEntryEvent, { kind: "existing" | "suggestion" }> };

const timeEntries = defineModel<TimeEntryContract[]>("timeEntries", { required: true });
// const timeEntrySuggestions = defineModel<TimeEntrySuggestionContract[]>("timeEntrySuggestions", { required: true });

const draftEvents = ref<TimeEntryEvent[]>([]);
const existingEvents = computed<TimeEntryEvent[]>(() =>
    timeEntries.value.map((x) => ({
        kind: "existing",
        color: "#7da6c9",
        start: x.startTime.getTime(),
        end: x.endTime.getTime(),
        timed: true,
        uiId: `event-uiId-${x.id}`,
        timeEntry: x
    }))
);

const events = computed<TimeEntryEvent[]>(() => [...existingEvents.value, ...draftEvents.value]);

const interaction = ref<Interaction>({ kind: "idle" });

const onEventCreate = async (event: TimeEntryEvent): Promise<TimeEntryContract> => {
    if (event.kind !== "draft") throw "Only draft can't be created";

    const newTimeEntry: TimeEntryContract = {
        id: "testId" as TimeEntryId,
        user: "testUser",
        endTime: new Date(event.end),
        startTime: new Date(event.start),
        taskId: event.createEntry.taskId
    };

    timeEntries.value.push(newTimeEntry);

    return newTimeEntry;
};

const onEventChanged = async (timeEntry: TimeEntryContract): Promise<TimeEntryContract> => {
    return timeEntry;
};

const addDraftEvent = (anchorStartMs: number) => {
    const newEvent: TimeEntryEvent = {
        kind: "draft",
        color: "#673AB7",
        start: anchorStartMs,
        end: anchorStartMs,
        timed: true,
        uiId: `event-uiId-${uuidv4()}`,
        createEntry: {
            endTime: new Date(anchorStartMs),
            startTime: new Date(anchorStartMs),
            taskId: ""
        }
    };

    draftEvents.value.push(newEvent);
    return newEvent;
};

const removeDraftEvent = (ev: TimeEntryEvent) => {
    if (ev.kind !== "draft") return;
    const i = draftEvents.value.indexOf(ev);
    if (i !== -1) draftEvents.value.splice(i, 1);
};

const confirmEvent = async () => {
    if (interaction.value.kind !== "create") return;

    if (interaction.value.event.kind !== "draft" || !interaction.value.event.createEntry.taskId) {
        cancelDraft();
        return;
    }

    await onEventCreate(interaction.value.event);
    removeDraftEvent(interaction.value.event);

    interaction.value = { kind: "idle" };
};

const cancelDraft = () => {
    if (interaction.value.kind !== "create") {
        interaction.value = { kind: "idle" };
        return;
    }

    removeDraftEvent(interaction.value.event);
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

    const newEvent = addDraftEvent(anchorStartMs);
    interaction.value = { kind: "draft", event: newEvent, anchorStartMs };
};

const updateInteractionFromPointer = (_nativeEvent: Event, tms: CalendarDayBodySlotScope) => {
    const mouseMs = toTime(tms);

    switch (interaction.value.kind) {
        case "move": {
            const { event, pointerOffsetMs } = interaction.value;
            if (pointerOffsetMs === undefined) return;

            const duration = event.end - event.start;
            const newStart = roundTime(mouseMs - pointerOffsetMs);

            event.start = newStart;
            event.timeEntry.startTime = new Date(newStart);
            event.end = newStart + duration;
            event.timeEntry.endTime = new Date(newStart + duration);
            return;
        }
        case "resize": {
            const { event } = interaction.value;
            const mouseRounded = roundTime(mouseMs, false);
            event.end = Math.max(mouseRounded, event.start);
            event.timeEntry.endTime = new Date(event.end);
            return;
        }
        case "draft": {
            const { event, anchorStartMs } = interaction.value;
            const mouseRounded = roundTime(mouseMs, false);
            event.start = Math.min(mouseRounded, anchorStartMs);
            event.createEntry.startTime = new Date(event.start);
            event.end = Math.max(mouseRounded, anchorStartMs);
            event.createEntry.endTime = new Date(event.end);
            return;
        }
        case "create":
        default:
            return;
    }
};

const finishInteraction = async () => {
    const cur = interaction.value;

    switch (cur.kind) {
        case "create":
            return;
        case "draft":
            interaction.value = { kind: "create", event: cur.event };
            return;
        case "move":
        case "resize": {
            interaction.value = { kind: "idle" };

            if (cur.event.kind === "existing") {
                const updated = await onEventChanged(cur.event.timeEntry);
                Object.assign(cur.event.timeEntry, updated);
            }
            return;
        }
        default:
            interaction.value = { kind: "idle" };
            return;
    }
};

const cancelInteractionOnLeave = () => {
    const cur = interaction.value;

    switch (cur.kind) {
        case "create":
            return;
        case "resize":
            cur.event.end = cur.originalEndMs;
            interaction.value = { kind: "idle" };
            return;
        case "draft":
            removeDraftEvent(cur.event);
            interaction.value = { kind: "idle" };
            return;
        default:
            interaction.value = { kind: "idle" };
            return;
    }
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
