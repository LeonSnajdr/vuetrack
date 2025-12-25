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
            <div :id="(event as TimeEntryEvent).uiId" :class="{ 'opacity-50': isInConflict(event as TimeEntryEvent) }" class="v-event-draggable">
                <p>{{ (event as TimeEntryEvent).start }}</p>
            </div>

            <div v-if="timed" @mousedown.stop="beginResizeEvent(event)" class="v-event-drag-bottom" />
        </template>
    </VCalendar>

    <!-- Create/Name Menu -->
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
                    autofocus
                />
                <div class="d-flex justify-end ga-2 mt-2">
                    <VBtn @click="cancelDraft" variant="text">Cancel</VBtn>
                    <VBtn @click="confirmEvent" color="primary">Save</VBtn>
                </div>
            </VCard>
        </VMenu>
    </template>

    <!-- Conflict Resolver Menu -->
    <template v-if="interaction.kind === 'conflict'">
        <VMenu :closeOnContentClick="false" :modelValue="true" :target="'#' + interaction.event.uiId" location="right" persistent>
            <VCard class="pa-3" width="350">
                <VCardTitle class="text-subtitle-1 text-error pa-0 mb-2"> Overlap Detected </VCardTitle>
                <VCardSubtitle class="pa-0 mb-3"> This event overlaps with {{ interaction.overlaps.length }} other(s). </VCardSubtitle>

                <VList density="compact" nav>
                    <VListItem
                        @click="resolveShift(false)"
                        prependIcon="mdi-arrow-up-thin"
                        subtitle="Shift up to next free gap"
                        title="Move to Previous Slot"
                    />
                    <VListItem @click="resolveShift(true)" prependIcon="mdi-arrow-down-thin" subtitle="Shift down to next free gap" title="Move to Next Slot" />
                    <VListItem @click="resolveTruncate" prependIcon="mdi-arrow-collapse-vertical" subtitle="Truncate this event to fit" title="Fit to Gap" />
                    <VListItem
                        @click="resolveForce"
                        class="text-error"
                        prependIcon="mdi-alert-box-outline"
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

type DraftTimeEntryEvent = Extract<TimeEntryEvent, { kind: "draft" }>;
type ExistingOrSuggestionTimeEntryEvent = Extract<TimeEntryEvent, { kind: "existing" | "suggestion" }>;

type Interaction =
    | { kind: "idle" }
    | {
          kind: "move";
          event: ExistingOrSuggestionTimeEntryEvent;
          pointerOffsetMs?: number;
          originalStartMs: number;
          originalEndMs: number;
      }
    | {
          kind: "resize";
          event: ExistingOrSuggestionTimeEntryEvent;
          originalEndMs: number;
      }
    | { kind: "draft"; event: DraftTimeEntryEvent; anchorStartMs: number }
    | { kind: "create"; event: DraftTimeEntryEvent }
    | {
          kind: "conflict";
          event: ExistingOrSuggestionTimeEntryEvent;
          originalStartMs: number;
          originalEndMs: number;
          overlaps: TimeEntryEvent[];
      };

const timeEntries = defineModel<TimeEntryContract[]>("timeEntries", { required: true });

const draftEvents = ref<TimeEntryEvent[]>([]);
const existingEvents = ref<TimeEntryEvent[]>([]);

watch(
    () => timeEntries.value.slice(),
    (newEntries, oldEntries) => {
        oldEntries ??= [];

        const added = newEntries.filter((x) => !oldEntries.includes(x));
        const removed = oldEntries.filter((x) => !newEntries.includes(x));

        added.forEach((x) => {
            existingEvents.value.push({
                kind: "existing",
                color: "#7da6c9",
                start: x.startTime.getTime(),
                end: x.endTime.getTime(),
                timed: true,
                uiId: `event-uiId-${uuidv4()}`,
                timeEntry: x
            });
        });

        existingEvents.value = existingEvents.value.filter((e) => e.kind === "existing" && !removed.includes(e.timeEntry));
    },
    { immediate: true, deep: true }
);

const events = computed<TimeEntryEvent[]>(() => [...existingEvents.value, ...draftEvents.value]);

const interaction = ref<Interaction>({ kind: "idle" });

const isInConflict = (event: TimeEntryEvent) => {
    return interaction.value.kind === "conflict" && interaction.value.event.uiId === event.uiId;
};

// --- DATA MANIPULATION HELPERS ---

const onEventCreate = async (event: DraftTimeEntryEvent) => {
    const newTimeEntry: TimeEntryContract = {
        id: "testId" as TimeEntryId,
        user: "testUser",
        endTime: new Date(event.end),
        startTime: new Date(event.start),
        taskId: event.createEntry.taskId
    };
    timeEntries.value.push(newTimeEntry);
};

// Called when we are 100% sure the event is valid
const onEventChanged = async (event: ExistingOrSuggestionTimeEntryEvent) => {
    event.timeEntry.startTime = new Date(event.start);
    event.timeEntry.endTime = new Date(event.end);
};

// Called when resolving conflicts by modifying OTHER events
const updateOtherEvent = (ev: TimeEntryEvent, newStart: number, newEnd: number) => {
    if (ev.kind === "existing") {
        ev.timeEntry.startTime = new Date(newStart);
        ev.timeEntry.endTime = new Date(newEnd);
        // Sync local visual state immediately
        ev.start = newStart;
        ev.end = newEnd;
    }
};

const deleteOtherEvent = (ev: TimeEntryEvent) => {
    if (ev.kind === "existing") {
        const idx = timeEntries.value.findIndex((t) => t === ev.timeEntry);
        if (idx !== -1) timeEntries.value.splice(idx, 1);
        // Visual removal happens via watcher
    }
};

// --- CONFLICT DETECTION ---

const getOverlappingEvents = (subject: TimeEntryEvent, candidates: TimeEntryEvent[]): TimeEntryEvent[] => {
    return candidates.filter((other) => {
        if (other.uiId === subject.uiId) return false;
        // Check overlap: StartA < EndB && EndA > StartB
        return subject.start < other.end && subject.end > other.start;
    });
};

// --- INTERACTION HANDLERS ---

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
    }
};

const finishInteraction = async () => {
    const cur = interaction.value;

    switch (cur.kind) {
        case "create":
        case "conflict":
            return;
        case "draft":
            // Drafts (new creations) can overlap? Usually allowed, but we can prevent if needed.
            // For now, flow to create menu.
            interaction.value = { kind: "create", event: cur.event };
            return;
        case "move":
        case "resize": {
            // CHECK OVERLAPS HERE
            const overlaps = getOverlappingEvents(cur.event, existingEvents.value);

            if (overlaps.length > 0) {
                // Determine original start for fallback.
                // For resize, start didn't change, but we need structure consistency.
                const origStart = cur.kind === "move" ? cur.originalStartMs : cur.event.start;
                const origEnd = cur.originalEndMs;

                interaction.value = {
                    kind: "conflict",
                    event: cur.event,
                    originalStartMs: origStart,
                    originalEndMs: origEnd,
                    overlaps: overlaps
                };
                return;
            }

            // No conflict
            interaction.value = { kind: "idle" };
            await onEventChanged(cur.event);
            return;
        }
        default:
            interaction.value = { kind: "idle" };
            return;
    }
};

// --- CONFLICT RESOLUTION LOGIC ---

// 1. Revert changes
const cancelConflict = () => {
    if (interaction.value.kind !== "conflict") return;
    const { event, originalStartMs, originalEndMs } = interaction.value;

    event.start = originalStartMs;
    event.end = originalEndMs;
    interaction.value = { kind: "idle" };
};

// 2. Truncate / Fit to Gap
const resolveTruncate = async () => {
    if (interaction.value.kind !== "conflict") return;
    const { event, overlaps } = interaction.value;

    // We only care about boundaries.
    // Logic: If I moved Down, my top hit someone's bottom, or my bottom hit someone's top.
    // Simplest approach: Find strict boundaries available around the current center.

    let allowedStart = event.start;
    let allowedEnd = event.end;

    overlaps.forEach((ov) => {
        // If overlap is completely "inside" the event, we can't just truncate.
        // But assuming user dragged *into* an overlap.

        // If overlap starts after our start, it cuts our tail
        if (ov.start > event.start && ov.start < event.end) {
            allowedEnd = Math.min(allowedEnd, ov.start);
        }
        // If overlap ends before our end, it cuts our head
        if (ov.end > event.start && ov.end < event.end) {
            allowedStart = Math.max(allowedStart, ov.end);
        }
        // If overlap covers us completely... well, duration becomes 0
    });

    if (allowedEnd <= allowedStart) {
        // Edge case: no space at all. Revert.
        cancelConflict();
        return;
    }

    event.start = allowedStart;
    event.end = allowedEnd;
    interaction.value = { kind: "idle" };
    await onEventChanged(event);
};

// 3. Move to next free slot (Up or Down)
const resolveShift = async (down: boolean) => {
    if (interaction.value.kind !== "conflict") return;
    const { event } = interaction.value;
    const duration = event.end - event.start;

    // Sort all events by time to find gaps
    const sorted = [...existingEvents.value]
        .filter((e) => e.uiId !== event.uiId) // Ignore self
        .sort((a, b) => a.start - b.start);

    // Filter to events on the same day (naive check: +/- 24 hours of current spot)
    const dayStart = new Date(event.start).setHours(0, 0, 0, 0);
    const dayEnd = new Date(event.start).setHours(23, 59, 59, 999);
    const dayEvents = sorted.filter((e) => e.end > dayStart && e.start < dayEnd);

    let foundStart = -1;

    if (down) {
        // Look forwards.
        // Start checking from the current colliding position's end, or current start?
        // Usually "Next Free" means after the stuff I collided with.
        // Let's sweep from current Start time forward.
        let searchTime = event.start;

        // Naive Sweep: check if (searchTime, searchTime+duration) overlaps anything.
        // If yes, jump to end of that overlap. Repeat.
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
        // Look backwards (Up)
        // Sweep backwards from current start.
        let searchTime = event.start;
        let safe = false;
        while (!safe && searchTime > dayStart) {
            const potentialStart = searchTime;
            const potentialEnd = searchTime + duration;

            const overlap = dayEvents.find((e) => potentialStart < e.end && potentialEnd > e.start);

            if (overlap) {
                // Jump before this overlap
                searchTime = overlap.start - duration;
            } else {
                safe = true;
                foundStart = searchTime;
            }
        }
    }

    if (foundStart !== -1) {
        event.start = foundStart;
        event.end = foundStart + duration;
        interaction.value = { kind: "idle" };
        await onEventChanged(event);
    } else {
        // Could not find a slot
        // Might want to show a snackbar here, but for now just revert
        cancelConflict();
    }
};

// 4. Force / Smash
const resolveForce = async () => {
    if (interaction.value.kind !== "conflict") return;
    const { event, overlaps } = interaction.value;

    // Logic: The dragged event wins.
    // All overlaps must be shrunk or deleted.

    for (const ov of overlaps) {
        // Case 1: Dragged event covers Ov completely -> Delete Ov
        if (event.start <= ov.start && event.end >= ov.end) {
            deleteOtherEvent(ov);
            continue;
        }

        // Case 2: Dragged overlaps Ov's tail (Ov Start ... Drag Start ... Ov End)
        // -> Shrink Ov to end at Drag Start
        if (event.start > ov.start && event.start < ov.end) {
            updateOtherEvent(ov, ov.start, event.start);
        }

        // Case 3: Dragged overlaps Ov's head (Ov Start ... Drag End ... Ov End)
        // -> Shrink Ov to start at Drag End
        if (event.end > ov.start && event.end < ov.end) {
            updateOtherEvent(ov, event.end, ov.end);
        }

        // Case 4: Dragged is inside Ov -> Split?
        // The user requirements said "make them smaller or remove". Splitting is complex.
        // Simple heuristic: Trim the smallest side of Ov to make space.
        if (event.start > ov.start && event.end < ov.end) {
            const headSize = event.start - ov.start;
            const tailSize = ov.end - event.end;
            if (headSize > tailSize) {
                updateOtherEvent(ov, ov.start, event.start); // Keep head
            } else {
                updateOtherEvent(ov, event.end, ov.end); // Keep tail
            }
        }
    }

    interaction.value = { kind: "idle" };
    await onEventChanged(event);
};

// --- EXISTING DRAFT HELPERS ---

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

const cancelInteractionOnLeave = () => {
    const cur = interaction.value;

    switch (cur.kind) {
        case "create":
        case "conflict": // Don't cancel menu on leave
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
