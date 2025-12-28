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
        <template #event="{ event }">
            <div v-if="isTimeEntryEvent(event)" :id="event.uiId">
                <div class="v-event-draggable">
                    <div v-if="event.kind === 'suggestion'">
                        <VIconBtn @click="acceptSuggestion" :icon="mdiCheck" />
                    </div>
                    <p v-if="event.kind === 'existing' || event.kind === 'suggestion'">{{ event.timeEntry.taskId }}</p>
                    <p v-else>Draft</p>
                    <p>{{ dateFormatter.format(event.start, "fullTime24h") }} - {{ dateFormatter.format(event.end, "fullTime24h") }}</p>
                </div>
                <div v-if="event.kind !== 'draft'" @mousedown.stop="beginResizeEvent(event)" class="v-event-drag-bottom" />
            </div>
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
                <template v-if="interaction.event.kind === 'draft'">
                    <VTextField
                    v-model.trim="interaction.event.createEntry.taskId"
                    @keydown.enter.prevent="confirmEvent"
                    @keydown.esc.prevent="cancelDraft"
                    class="mt-3"
                    density="compact"
                    label="Taskid"
                    autofocus
                    />
                    <div class="d-flex justify-end ga-2 mt-2">
                        <VBtn @click="cancelDraft" variant="text">Cancel</VBtn>
                        <VBtn @click="confirmEvent" color="primary">Save</VBtn>
                    </div>
                </template>
            </VCard>
        </VMenu>
    </template>

    <template v-if="interaction.kind === 'conflict'">
        <VMenu :closeOnContentClick="false" :modelValue="true" :target="'#' + interaction.event.uiId" location="right" persistent>
            <VCard class="pa-3" width="350">
                <VCardTitle class="text-subtitle-1 text-error pa-0 mb-2"> Overlap Detected </VCardTitle>
                <VCardSubtitle class="pa-0 mb-3"> This event overlaps with {{ interaction.overlaps.length }} other(s). </VCardSubtitle>

                <VList density="compact" nav>
                    <VListItem @click="resolveShift(false)" :prependIcon="mdiArrowUpThin" subtitle="Shift up to next free gap" title="Move to Previous Slot" />
                    <VListItem @click="resolveShift(true)" :prependIcon="mdiArrowDownThin" subtitle="Shift down to next free gap" title="Move to Next Slot" />
                    <VListItem @click="resolveTruncate" :prependIcon="mdiArrowCollapseVertical" subtitle="Truncate this event to fit" title="Fit to Gap" />
                    <VListItem
                        @click="resolveForce"
                        :prependIcon="mdiAlertBoxOutline"
                        class="text-error"
                        subtitle="Shrink or remove conflicting events"
                        title="Force Position"
                    />
                </VList>

                <div class="d-flex justify-end mt-2 border-t pt-2">
                    <VBtn @click="cancelConflict" size="small" variant="plain">Cancel</VBtn>
                </div>
            </VCard>
        </VMenu>
    </template>
</template>

<script setup lang="ts">
import type { EventSlotScope } from "vuetify/lib/components/VCalendar/VCalendar.mjs";
import type { CalendarDayBodySlotScope, CalendarEvent } from "vuetify/lib/components/VCalendar/types.mjs";
import { isTimeEntryEvent, type Interaction, type SuggestionTimeEntryEvent, type TimeEntryEvent } from "./types";
import useMappingToEvents from "./timeEntryEventSync";

const timeEntries = defineModel<TimeEntryContract[]>("timeEntries", { required: true });
const timeEntrySuggestions = defineModel<TimeEntrySuggestionContract[]>("timeEntrySuggestions", { required: true });

const existingEvents = useMappingToEvents("existing", timeEntries);
const suggestionEvents = useMappingToEvents("suggestion", timeEntrySuggestions, "#22C55E");
const draftEvents = ref<TimeEntryEvent[]>([]);

const events = computed<TimeEntryEvent[]>(() => [...existingEvents.value, ...suggestionEvents.value, ...draftEvents.value]);

const interaction = ref<Interaction>({ kind: "idle" });

const dateFormatter = useDate();

const acceptSuggestion = (event: SuggestionTimeEntryEvent) => {
    interaction.value =  { kind: "create", event: event }
}

const upsertEvent = async (event: TimeEntryEvent) => {
    const startTime = new Date(event.start);
    const endTime = new Date(event.end);

    if (event.kind === "draft") {
        if (!event.createEntry.taskId) return; // Basic validation

        const newTimeEntry: TimeEntryContract = {
            id: "testId" as TimeEntryId,
            user: "testUser",
            startTime: startTime,
            endTime: endTime,
            taskId: event.createEntry.taskId
        };

        timeEntries.value.push(newTimeEntry);

        removeEvent(event);
    } else if (event.kind === "existing") {
        event.timeEntry.startTime = startTime;
        event.timeEntry.endTime = endTime;
        console.log("Event updated", event.uiId);
    } else if (event.kind === "suggestion") {
        event.timeEntry.startTime = startTime;
        event.timeEntry.endTime = endTime;
        console.log("Suggestion updated");
    }
};

const removeEvent = (event: TimeEntryEvent) => {
    if (event.kind === "draft") {
        const idx = draftEvents.value.indexOf(event);
        if (idx !== -1) draftEvents.value.splice(idx, 1);
    } else if (event.kind === "existing") {
        const idx = timeEntries.value.indexOf(event.timeEntry);
        if (idx !== -1) timeEntries.value.splice(idx, 1);
    } else if (event.kind === "suggestion") {
        const idx = timeEntrySuggestions.value.indexOf(event.timeEntry);
        if (idx !== -1) timeEntrySuggestions.value.splice(idx, 1);
    }

    console.log("Event removed", event.uiId);
};

const getOverlappingEvents = (subject: TimeEntryEvent, candidates: TimeEntryEvent[]): TimeEntryEvent[] => {
    return candidates.filter((other) => {
        if (other.uiId === subject.uiId) return false;
        return subject.start < other.end && subject.end > other.start;
    });
};

const beginMoveEvent = (_nativeEvent: Event, { event, timed }: EventSlotScope) => {
    if (interaction.value.kind === "create" || interaction.value.kind === "conflict") return;
    if (!event || !timed) return;

    const ev = event as TimeEntryEvent;
    if (ev.kind === "draft") return;

    interaction.value = {
        kind: "move",
        event: ev,
        pointerOffsetMs: undefined,
        originalStartMs: ev.start,
        originalEndMs: ev.end
    };
};

const beginResizeEvent = (event: CalendarEvent) => {
    if (interaction.value.kind === "create" || interaction.value.kind === "conflict") return;

    const ev = event as TimeEntryEvent;
    if (ev.kind === "draft") return;

    interaction.value = { kind: "resize", event: ev, originalEndMs: ev.end };
};

const beginGridInteraction = (_nativeEvent: Event, tms: CalendarDayBodySlotScope) => {
    if (interaction.value.kind === "create" || interaction.value.kind === "conflict") return;

    const mouseMs = toTime(tms);

    if (interaction.value.kind === "move" && interaction.value.pointerOffsetMs === undefined) {
        interaction.value.pointerOffsetMs = mouseMs - interaction.value.event.start;
        return;
    }

    const anchorStartMs = roundTime(mouseMs);

    // 1. Prevent starting on top of an existing event
    const overlapping = existingEvents.value.some((e) => anchorStartMs >= e.start && anchorStartMs < e.end);
    if (overlapping) return;

    // 2. Determine boundaries to prevent dragging over existing events
    // Find neighbors to set limits
    const neighbors = [...existingEvents.value].sort((a, b) => a.start - b.start);
    const nextEvent = neighbors.find((e) => e.start >= anchorStartMs);
    const prevEvent = [...neighbors].reverse().find((e) => e.end <= anchorStartMs);

    // If no event is after, we can go to infinity (or day end), if no event is before, we go to 0
    const maxEndMs = nextEvent ? nextEvent.start : Number.MAX_SAFE_INTEGER;
    const minStartMs = prevEvent ? prevEvent.end : 0;

    const newEvent = addDraftEvent(anchorStartMs);
    interaction.value = {
        kind: "draft",
        event: newEvent,
        anchorStartMs,
        minStartMs,
        maxEndMs
    };
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
            const mouseRounded = roundTime(mouseMs, false);
            event.end = Math.max(mouseRounded, event.start);
            return;
        }
        case "draft": {
            const { event, anchorStartMs, minStartMs, maxEndMs } = interaction.value;
            let mouseRounded = roundTime(mouseMs, false);

            // 3. Clamp dragging within constraints
            if (mouseRounded > maxEndMs) {
                mouseRounded = maxEndMs;
            }
            if (mouseRounded < minStartMs) {
                mouseRounded = minStartMs;
            }

            event.start = Math.min(mouseRounded, anchorStartMs);
            event.end = Math.max(mouseRounded, anchorStartMs);
            return;
        }
    }
};

const finishInteraction = async () => {
    const cur = interaction.value;

    switch (cur.kind) {
        case "create":
        case "conflict":
            return;
        case "draft":
            // Drafts should now be valid without overlaps due to constraints in creation
            interaction.value = { kind: "create", event: cur.event };
            return;
        case "move":
        case "resize": {
            if (cur.event.kind === "existing") {
                const overlaps = getOverlappingEvents(cur.event, existingEvents.value);

                if (overlaps.length > 0) {
                    // Determine original start for fallback.
                    const origStart = cur.kind === "move" ? cur.originalStartMs : cur.event.start;
                    const origEnd = cur.originalEndMs;

                    interaction.value = {
                        kind: "conflict",
                        event: cur.event,
                        overlaps: overlaps,
                        onResolved: async (position) => {
                            cur.event.start = position.start;
                            cur.event.end = position.end;
                            await upsertEvent(cur.event);
                            interaction.value = { kind: "idle" };
                        },
                        onCanceled: async () => {
                            cur.event.start = origStart;
                            cur.event.end = origEnd;
                            interaction.value = { kind: "idle" };
                        }
                    };
                    return;
                }
            }

            interaction.value = { kind: "idle" };
            await upsertEvent(cur.event);
            return;
        }
        default:
            interaction.value = { kind: "idle" };
            return;
    }
};

// 2. Truncate / Fit to Gap
const resolveTruncate = async () => {
    if (interaction.value.kind !== "conflict") return;
    const { event, overlaps, onResolved, onCanceled } = interaction.value;

    let allowedStart = event.start;
    let allowedEnd = event.end;

    // Check if the event is completely inside any of the overlapping events
    const isFullyContained = overlaps.some((ov) => ov.start <= event.start && ov.end >= event.end);

    if (isFullyContained) {
        await onCanceled();
        return;
    }

    overlaps.forEach((ov) => {
        if (ov.start > event.start && ov.start < event.end) {
            allowedEnd = Math.min(allowedEnd, ov.start);
        }
        if (ov.end > event.start && ov.end < event.end) {
            allowedStart = Math.max(allowedStart, ov.end);
        }
    });

    if (allowedEnd <= allowedStart) {
        await onCanceled();
        return;
    }

    await onResolved({ start: allowedStart, end: allowedEnd })
};

// 3. Move to next free slot (Up or Down)
const resolveShift = async (down: boolean) => {
    if (interaction.value.kind !== "conflict") return;
    const { event, onResolved, onCanceled } = interaction.value;
    const duration = event.end - event.start;

    const sorted = [...existingEvents.value].filter((e) => e.uiId !== event.uiId).sort((a, b) => a.start - b.start);

    const dayStart = new Date(event.start).setHours(0, 0, 0, 0);
    const dayEnd = new Date(event.start).setHours(23, 59, 59, 999);
    const dayEvents = sorted.filter((e) => e.end > dayStart && e.start < dayEnd);

    let foundStart = -1;

    if (down) {
        let searchTime = event.start;
        let safe = false;
        while (!safe && searchTime < dayEnd) {
            const overlap = dayEvents.find((e) => searchTime < e.end && searchTime + duration > e.start);
            if (overlap) {
                searchTime = overlap.end;
            } else {
                safe = true;
                foundStart = searchTime;
            }
        }
    } else {
        let searchTime = event.start;
        let safe = false;
        while (!safe && searchTime > dayStart) {
            const potentialStart = searchTime;
            const potentialEnd = searchTime + duration;
            const overlap = dayEvents.find((e) => potentialStart < e.end && potentialEnd > e.start);
            if (overlap) {
                searchTime = overlap.start - duration;
            } else {
                safe = true;
                foundStart = searchTime;
            }
        }
    }

    if (foundStart !== -1) {
        await onResolved({ start: foundStart, end: foundStart + duration })
    } else {
        await onCanceled();
    }
};

// 4. Force / Smash
const resolveForce = async () => {
    if (interaction.value.kind !== "conflict") return;
    const { event, overlaps, onResolved } = interaction.value;

    for (const ov of overlaps) {
        // Case 1: Dragged event covers Ov completely -> Delete Ov
        if (event.start <= ov.start && event.end >= ov.end) {
            removeEvent(ov);
            continue;
        }

        // Case 2: Dragged overlaps Ov's tail (Ov Start ... Drag Start ... Ov End)
        if (event.start > ov.start && event.start < ov.end) {
            ov.end = event.start;
            upsertEvent(ov);
        }

        // Case 3: Dragged overlaps Ov's head (Ov Start ... Drag End ... Ov End)
        if (event.end > ov.start && event.end < ov.end) {
            ov.start = event.end;
            upsertEvent(ov);
        }

        // Case 4: Dragged is inside Ov
        if (event.start > ov.start && event.end < ov.end) {
            const headSize = event.start - ov.start;
            const tailSize = ov.end - event.end;
            if (headSize > tailSize) {
                ov.end = event.start; // Keep head
            } else {
                ov.start = event.end; // Keep tail
            }
            upsertEvent(ov);
        }
    }

    await onResolved({ start: event.start, end: event.end });
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

const confirmEvent = async () => {
    if (interaction.value.kind !== "create") return;
    await upsertEvent(interaction.value.event);
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

const cancelInteractionOnLeave = () => {
    const cur = interaction.value;

    switch (cur.kind) {
        case "create":
        case "conflict":
            return;
        case "resize":
            cur.event.end = cur.originalEndMs;
            interaction.value = { kind: "idle" };
            return;
        case "move":
            cur.event.start = cur.originalStartMs;
            cur.event.end = cur.originalEndMs;
            interaction.value = { kind: "idle" };
            return;
        case "draft":
            removeEvent(cur.event);
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
