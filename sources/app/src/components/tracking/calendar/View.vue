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
            <VHover v-if="isTimeEntryEvent(event)" v-slot="{ isHovering, props }">
                <div :id="event.uiId" v-bind="props" class="h-100 pa-2 text-truncate">
                    <div v-show="isHovering && interaction.kind === 'idle'" class="position-absolute d-flex ga-2" style="top: 5px; right: 5px">
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
                            @click.stop="beginEdit(event)"
                            @mousedown.stop
                            :icon="mdiPencil"
                            iconColor="white"
                            variant="flat"
                        />
                    </div>
                    <p v-if="event.kind === 'existing' || event.kind === 'suggestion'">{{ event.timeEntry.taskId }}</p>
                    <p v-else>{{ $t("calendar.event.draft") }}</p>
                    <p>{{ dateFormatter.format(event.start, "fullTime24h") }} - {{ dateFormatter.format(event.end, "fullTime24h") }}</p>
                </div>
                <div @mousedown.stop="beginResizeEvent(event)" class="v-event-drag-bottom" />
            </VHover>
        </template>
    </VCalendar>

    <TrackingCalendarFeaturesEventDialog
        v-if="interaction.kind === 'create'"
        v-model:event="interaction.event"
        @cancel="cancelCreate"
        @confirm="confirmEvent"
        :loading="createLoading"
    />

    <TrackingCalendarFeaturesEventDialog
        v-if="interaction.kind === 'edit'"
        v-model:event="interaction.event"
        @cancel="cancelEdit"
        @confirm="confirmEdit"
        :loading="editLoading"
    />

    <TrackingCalendarFeaturesConflictDialog
        v-if="interaction.kind === 'conflict'"
        v-model:loadingStrategyId="conflictLoadingId"
        @canceled="handleConflictCanceled"
        @resolved="handleConflictResolved"
        :allEvents="existingEvents"
        :event="interaction.event"
        :overlaps="interaction.overlaps"
    />
</template>

<script setup lang="ts">
import type { EventSlotScope } from "vuetify/lib/components/VCalendar/VCalendar.mjs";
import type { CalendarDayBodySlotScope, CalendarEvent } from "vuetify/lib/components/VCalendar/types.mjs";
import {
    isTimeEntryEvent,
    type DraftTimeEntryEvent,
    type ExistingTimeEntryEvent,
    type Interaction,
    type SuggestionTimeEntryEvent,
    type TimeEntryEvent
} from "./types";
import { createExistingEventWrapper, createSuggestionEventWrapper, createDraftEvent } from "./createEventWrapper";
import type { ConflictResolutionResult, EventMutation } from "./features/types";

const timeEntryStore = useTimeEntryStore();
const timeEntrySuggestionStore = useTimeEntrySuggestionStore();

const { timeEntries } = storeToRefs(timeEntryStore);
const { timeEntrySuggestions } = storeToRefs(timeEntrySuggestionStore);

const existingEvents = computed(() => timeEntries.value.map((c) => createExistingEventWrapper(c)));
const suggestionEvents = computed(() => timeEntrySuggestions.value.map((c) => createSuggestionEventWrapper(c)));
const draftEvents = ref<DraftTimeEntryEvent[]>([]);

const events = computed<TimeEntryEvent[]>(() => [...existingEvents.value, ...suggestionEvents.value, ...draftEvents.value]);

const interaction = ref<Interaction>({ kind: "idle" });

const createLoading = ref(false);
const editLoading = ref(false);
const conflictLoadingId = ref<string | null>(null);

const handleConflictResolved = async (result: ConflictResolutionResult) => {
    if (interaction.value.kind !== "conflict") return;
    await applyMutations(result.mutations);
    await interaction.value.onResolved(result.position);
    conflictLoadingId.value = null;
};

const handleConflictCanceled = () => {
    if (interaction.value.kind !== "conflict") return;
    interaction.value.onCanceled();
};

const applyMutations = async (mutations?: EventMutation[]) => {
    const promises = (mutations ?? []).map(async (m) => {
        if (m.action === "remove") {
            await removeEvent(m.event);
        } else if (m.action === "update") {
            const result = await updateEvent(m.event, { start: m.start, end: m.end });
            if (result.status === "success") {
                m.event.start = m.start;
                m.event.end = m.end;
            }
        }
    });
    await Promise.all(promises);
};

const dateFormatter = useDate();

const acceptSuggestion = (event: SuggestionTimeEntryEvent) => {
    interaction.value = { kind: "create", event: event };
};

const beginEdit = (event: ExistingTimeEntryEvent | SuggestionTimeEntryEvent) => {
    interaction.value = { kind: "edit", event };
};

const confirmEdit = async (event: TimeEntryEvent) => {
    if (event.kind === "draft") return;
    editLoading.value = true;
    await updateEvent(event);
    editLoading.value = false;
    interaction.value = { kind: "idle" };
};

const cancelEdit = () => {
    interaction.value = { kind: "idle" };
};

const createEvent = async (event: TimeEntryEvent, position?: { start: number; end: number }) => {
    const startTime = new Date(position?.start ?? event.start);
    const endTime = new Date(position?.end ?? event.end);

    if (event.kind === "draft") {
        if (!event.createEntry.taskId) {
            removeEvent(event);
            return false;
        }

        const result = await timeEntryStore.create({
            startTime,
            endTime,
            taskId: event.createEntry.taskId
        });

        removeEvent(event);

        return result.status === "success";
    } else if (event.kind === "suggestion") {
        const result = await timeEntryStore.create({
            startTime,
            endTime,
            taskId: event.timeEntry.taskId
        });

        if (result.status === "success") {
            await timeEntrySuggestionStore.dismiss(event.timeEntry.id);
        }
        return result.status === "success";
    }
    return false;
};

const updateEvent = async (event: TimeEntryEvent, position?: { start: number; end: number }, originalPositionMs?: { start: number; end: number }) => {
    const startTime = new Date(position?.start ?? event.start);
    const endTime = new Date(position?.end ?? event.end);

    let result: ActionResult<unknown> = error();
    if (event.kind === "existing") {
        result = await timeEntryStore.update(event.timeEntry.id, { startTime, endTime, taskId: event.timeEntry.taskId });
    } else if (event.kind === "suggestion") {
        result = await timeEntrySuggestionStore.update(event.timeEntry.id, { startTime, endTime, taskId: event.timeEntry.taskId });
    }

    // Only revert on actual error, not on cancellation
    if (result.status === "error" && originalPositionMs) {
        event.start = originalPositionMs.start;
        event.end = originalPositionMs.end;
    }
    return result;
};

const removeEvent = async (event: TimeEntryEvent) => {
    if (event.kind === "draft") {
        const idx = draftEvents.value.indexOf(event);
        if (idx !== -1) draftEvents.value.splice(idx, 1);
    } else if (event.kind === "existing") {
        await timeEntryStore.remove(event.timeEntry.id);
    } else if (event.kind === "suggestion") {
        await timeEntrySuggestionStore.dismiss(event.timeEntry.id);
    }
};

const cancelPendingUpdateForEvent = (event: TimeEntryEvent) => {
    if (event.kind === "existing") {
        timeEntryStore.cancelPendingUpdate(event.timeEntry.id);
    } else if (event.kind === "suggestion") {
        timeEntrySuggestionStore.cancelPendingUpdate(event.timeEntry.id);
    }
};

const getOverlappingEvents = (subject: TimeEntryEvent, candidates: TimeEntryEvent[]): TimeEntryEvent[] => {
    return candidates.filter((other) => {
        if (other.uiId === subject.uiId) return false;
        return subject.start < other.end && subject.end > other.start;
    });
};

const beginMoveEvent = (_nativeEvent: Event, { event, timed }: EventSlotScope) => {
    if (interaction.value.kind === "create" || interaction.value.kind === "edit" || interaction.value.kind === "conflict") return;
    if (!event || !timed) return;

    const ev = event as TimeEntryEvent;
    cancelPendingUpdateForEvent(ev);

    interaction.value = {
        kind: "move",
        event: ev,
        pointerOffsetMs: undefined,
        originalStartMs: ev.start,
        originalEndMs: ev.end
    };
};

const beginResizeEvent = (event: CalendarEvent) => {
    if (interaction.value.kind === "create" || interaction.value.kind === "edit" || interaction.value.kind === "conflict") return;

    const ev = event as TimeEntryEvent;
    cancelPendingUpdateForEvent(ev);

    interaction.value = { kind: "resize", event: ev, originalEndMs: ev.end };
};

const beginGridInteraction = (_nativeEvent: Event, tms: CalendarDayBodySlotScope) => {
    if (interaction.value.kind === "create" || interaction.value.kind === "edit" || interaction.value.kind === "conflict") return;

    const mouseMs = toTime(tms);

    if (interaction.value.kind === "move" && interaction.value.pointerOffsetMs === undefined) {
        interaction.value.pointerOffsetMs = mouseMs - interaction.value.event.start;
        return;
    }

    const anchorStartMs = roundTime(mouseMs);
    const newEvent = createDraftEvent(anchorStartMs);
    draftEvents.value.push(newEvent);

    interaction.value = {
        kind: "draft",
        event: newEvent,
        anchorStartMs
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
        case "edit":
        case "conflict":
            return;
        case "draft":
            interaction.value = { kind: "create", event: cur.event };
            return;
        case "move":
        case "resize": {
            if (cur.event.kind === "existing") {
                const overlaps = getOverlappingEvents(cur.event, existingEvents.value);

                if (overlaps.length > 0) {
                    const origStartMs = cur.kind === "move" ? cur.originalStartMs : cur.event.start;
                    const origEndMs = cur.originalEndMs;

                    interaction.value = {
                        kind: "conflict",
                        event: cur.event,
                        overlaps: overlaps,
                        onResolved: async (position) => {
                            const result = await updateEvent(cur.event, position, { start: origStartMs, end: origEndMs });
                            if (result.status === "success") {
                                cur.event.start = position.start;
                                cur.event.end = position.end;
                            }
                            interaction.value = { kind: "idle" };
                        },
                        onCanceled: async () => {
                            cur.event.start = origStartMs;
                            cur.event.end = origEndMs;
                            interaction.value = { kind: "idle" };
                        }
                    };
                    return;
                }
            }

            const origStartMs = cur.kind === "move" ? cur.originalStartMs : cur.event.start;
            const origEndMs = cur.originalEndMs;

            interaction.value = { kind: "idle" };
            await updateEvent(cur.event, undefined, { start: origStartMs, end: origEndMs });
            return;
        }
        default:
            interaction.value = { kind: "idle" };
            return;
    }
};

const confirmEvent = async (event: TimeEntryEvent) => {
    if (interaction.value.kind !== "create") return;
    if (event.kind === "existing") return;

    const overlaps = getOverlappingEvents(event, existingEvents.value);

    if (overlaps.length > 0) {
        interaction.value = {
            kind: "conflict",
            event: event,
            overlaps: overlaps,
            onResolved: async (position) => {
                await createEvent(event, position);
                interaction.value = { kind: "idle" };
            },
            onCanceled: async () => {
                interaction.value = { kind: "idle" };
            }
        };
        return;
    }

    createLoading.value = true;
    await createEvent(event);
    createLoading.value = false;
    interaction.value = { kind: "idle" };
};

const cancelCreate = () => {
    if (interaction.value.kind !== "create") return;

    const ev = interaction.value.event;
    if (ev.kind === "draft") {
        removeEvent(ev);
    }

    interaction.value = { kind: "idle" };
};

const cancelInteractionOnLeave = () => {
    const cur = interaction.value;

    switch (cur.kind) {
        case "create":
        case "edit":
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
