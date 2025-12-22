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
            <div class="v-event-draggable">
                <component :is="eventSummary" />
            </div>

            <div v-if="timed" @mousedown.stop="beginResizeEvent(event)" class="v-event-drag-bottom" />
        </template>
    </VCalendar>
</template>

<script setup lang="ts">
import type { CalendarDayBodySlotScope, CalendarEvent } from "vuetify/lib/components/VCalendar/types.mjs";
import type { EventSlotScope } from "vuetify/lib/components/VCalendar/VCalendar.mjs";

const events = ref<CalendarEvent[]>([]);

type Interaction =
    | { kind: "idle" }
    | { kind: "move"; event: CalendarEvent; pointerOffsetMs?: number }
    | { kind: "resize"; event: CalendarEvent; originalEndMs: number }
    | { kind: "create"; event: CalendarEvent; anchorStartMs: number };

const interaction = ref<Interaction>({ kind: "idle" });

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

const beginMoveEvent = (_nativeEvent: Event, { event, timed }: EventSlotScope) => {
    if (!event || !timed) return;

    interaction.value = { kind: "move", event, pointerOffsetMs: undefined };
};

const beginResizeEvent = (event: CalendarEvent) => {
    interaction.value = { kind: "resize", event, originalEndMs: event.end };
};

const beginGridInteraction = (_nativeEvent: Event, tms: CalendarDayBodySlotScope) => {
    const mouseMs = toTime(tms);

    // If we are moving an existing event, compute cursor offset on first grid press.
    if (interaction.value.kind === "move" && interaction.value.pointerOffsetMs === undefined) {
        interaction.value.pointerOffsetMs = mouseMs - interaction.value.event.start;
        return;
    }

    // Otherwise, start create interaction on empty grid
    const anchorStartMs = roundTime(mouseMs);

    const newEvent: CalendarEvent = {
        name: `Event #${events.value.length}`,
        color: "#673AB7",
        start: anchorStartMs,
        end: anchorStartMs,
        timed: true
    };

    events.value.push(newEvent);
    interaction.value = { kind: "create", event: newEvent, anchorStartMs };
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
            event.end = newStart + duration;
            return;
        }
        case "resize": {
            const { event } = interaction.value;

            // Keep start fixed, resize end with mouse (rounded up)
            const mouseRounded = roundTime(mouseMs, false);
            event.end = Math.max(mouseRounded, event.start);
            return;
        }
        case "create": {
            const { event, anchorStartMs } = interaction.value;

            const mouseRounded = roundTime(mouseMs, false);
            event.start = Math.min(mouseRounded, anchorStartMs);
            event.end = Math.max(mouseRounded, anchorStartMs);
            return;
        }
        case "idle":
        default:
            return;
    }
};

const finishInteraction = () => {
    interaction.value = { kind: "idle" };
};

const cancelInteractionOnLeave = () => {
    const current = interaction.value;

    // revert to original end
    if (current.kind === "resize") {
        current.event.end = current.originalEndMs;
    }

    // remove the new event
    if (current.kind === "create") {
        const i = events.value.indexOf(current.event);
        if (i !== -1) events.value.splice(i, 1);
    }

    // move: keep the moved position

    interaction.value = { kind: "idle" };
};

const roundTime = (time: number, down = true) => {
    const roundTo = 15; // minutes
    const step = roundTo * 60 * 1000;

    return down ? time - (time % step) : time + (step - (time % step));
};

const toTime = (tms: CalendarDayBodySlotScope) => {
    return new Date(tms.year, tms.month - 1, tms.day, tms.hour, tms.minute).getTime();
};
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
