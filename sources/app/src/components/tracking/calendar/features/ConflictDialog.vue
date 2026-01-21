<template>
    <VMenu :closeOnContentClick="false" :target="targetSelector" location="right" modelValue persistent>
        <VCard width="350">
            <VCardTitle class="text-subtitle-1 mb-n5">{{ $t("calendar.conflict.title") }}</VCardTitle>
            <VCardSubtitle>
                <p class="text-medium-emphasis text-caption">{{ $t("calendar.conflict.subtitle", { count: interaction.overlaps.length }) }}</p>
            </VCardSubtitle>
            <VCardText>
                <div class="d-flex flex-column ga-2">
                    <VBtn
                        v-for="strategy in defaultStrategies"
                        :key="strategy.id"
                        @click="executeStrategy(strategy)"
                        :color="strategy.variant"
                        :disabled="conflictLoadingId !== null"
                        :loading="conflictLoadingId === strategy.id"
                        :prependIcon="strategy.icon"
                        variant="tonal"
                    >
                        {{ strategy.label }}
                    </VBtn>
                </div>
            </VCardText>
            <VCardActions>
                <VSpacer />
                <VBtn @click="conflict.cancel()" :disabled="conflictLoadingId !== null" size="small" variant="plain">{{ $t("action.cancel") }}</VBtn>
            </VCardActions>
        </VCard>
    </VMenu>
</template>

<script setup lang="ts">
import type {
    Interaction,
    TimeEntryEvent,
    TimeEntryMutation,
    ExistingTimeEntryDeleteMutation,
    ExistingTimeEntryUpdateMutation,
    SuggestionTimeEntryDeleteMutation,
    SuggestionTimeEntryUpdateMutation,
    DraftTimeEntryDeleteMutation
} from "@/components/tracking/calendar/types";
import { useConflict } from "@/components/tracking/calendar/composables/useConflict";

export interface ConflictResolutionStrategy {
    id: string;
    label: string;
    subtitle: string;
    icon: string;
    variant?: "error" | "warning";
    resolve: () => TimeEntryMutation[] | null;
}

const interaction = defineModel<Extract<Interaction, { kind: "conflict" }>>("interaction", { required: true });

const conflict = useConflict();
const calendarStore = useCalendarStore();
const { conflictLoadingId, existingEvents } = storeToRefs(calendarStore);

const { t } = useI18n();

const targetSelector = computed(() => "#" + interaction.value.event.uiId);

// Helper to create a mutation with updated position
const updateMutationPosition = (mutation: TimeEntryMutation, start: number, end: number): TimeEntryMutation => {
    if (mutation.kind === "update") {
        return {
            ...mutation,
            update: {
                ...mutation.update,
                startTime: new Date(start),
                endTime: new Date(end)
            }
        };
    } else if (mutation.kind === "create") {
        return {
            ...mutation,
            create: {
                ...mutation.create,
                startTime: new Date(start),
                endTime: new Date(end)
            }
        };
    }
    return mutation;
};

// Helper to create delete mutation for an event
const createDeleteMutation = (event: TimeEntryEvent): TimeEntryMutation => {
    if (event.kind === "draft") {
        return { kind: "delete", event } as DraftTimeEntryDeleteMutation;
    } else if (event.kind === "existing") {
        return { kind: "delete", event, id: event.timeEntry.id } as ExistingTimeEntryDeleteMutation;
    } else {
        return { kind: "delete", event, id: event.timeEntry.id } as SuggestionTimeEntryDeleteMutation;
    }
};

// Helper to create update mutation for an event
const createUpdateMutation = (event: TimeEntryEvent, newStart: number, newEnd: number): TimeEntryMutation | null => {
    if (event.kind === "existing") {
        return {
            kind: "update",
            event,
            update: {
                startTime: new Date(newStart),
                endTime: new Date(newEnd),
                taskId: event.timeEntry.taskId
            },
            originalPosition: { start: event.start, end: event.end }
        } as ExistingTimeEntryUpdateMutation;
    } else if (event.kind === "suggestion") {
        return {
            kind: "update",
            event,
            update: {
                startTime: new Date(newStart),
                endTime: new Date(newEnd),
                taskId: event.timeEntry.taskId
            },
            originalPosition: { start: event.start, end: event.end }
        } as SuggestionTimeEntryUpdateMutation;
    }
    return null;
};

const defaultStrategies = computed<ConflictResolutionStrategy[]>(() => [
    {
        id: "shift-up",
        label: t("calendar.conflict.strategy.movePrevious"),
        subtitle: t("calendar.conflict.strategy.movePrevious.subtitle"),
        icon: mdiArrowUpThin,
        resolve: resolveShiftUp
    },
    {
        id: "shift-down",
        label: t("calendar.conflict.strategy.moveNext"),
        subtitle: t("calendar.conflict.strategy.moveNext.subtitle"),
        icon: mdiArrowDownThin,
        resolve: resolveShiftDown
    },
    {
        id: "truncate",
        label: t("calendar.conflict.strategy.fitToGap"),
        subtitle: t("calendar.conflict.strategy.fitToGap.subtitle"),
        icon: mdiArrowCollapseVertical,
        resolve: resolveTruncate
    },
    {
        id: "force",
        label: t("calendar.conflict.strategy.forcePosition"),
        subtitle: t("calendar.conflict.strategy.forcePosition.subtitle"),
        icon: mdiAlertBoxOutline,
        variant: "error",
        resolve: resolveForce
    }
]);

const executeStrategy = async (strategy: ConflictResolutionStrategy) => {
    conflictLoadingId.value = strategy.id;

    const mutations = strategy.resolve();
    if (mutations) {
        await conflict.finish(mutations);
    } else {
        conflictLoadingId.value = null;
        await conflict.cancel();
    }
};

// Default strategies
const resolveShiftUp = (): TimeEntryMutation[] | null => {
    const event = interaction.value.event;
    const mutation = interaction.value.mutation;
    const duration = event.end - event.start;

    const sorted = [...existingEvents.value].filter((e) => e.uiId !== event.uiId).sort((a, b) => a.start - b.start);

    const dayStart = new Date(event.start).setHours(0, 0, 0, 0);
    const dayEnd = new Date(event.start).setHours(23, 59, 59, 999);
    const dayEvents = sorted.filter((e) => e.end > dayStart && e.start < dayEnd);

    let searchTime = event.start;
    let foundStart = -1;

    while (searchTime > dayStart) {
        const potentialStart = searchTime;
        const potentialEnd = searchTime + duration;
        const overlap = dayEvents.find((e) => potentialStart < e.end && potentialEnd > e.start);
        if (overlap) {
            searchTime = overlap.start - duration;
        } else {
            foundStart = searchTime;
            break;
        }
    }

    if (foundStart !== -1) {
        const foundEnd = foundStart + duration;
        const updatedMutation = updateMutationPosition(mutation, foundStart, foundEnd);
        return [updatedMutation];
    }
    return null;
};

const resolveShiftDown = (): TimeEntryMutation[] | null => {
    const event = interaction.value.event;
    const mutation = interaction.value.mutation;
    const duration = event.end - event.start;

    const sorted = [...existingEvents.value].filter((e) => e.uiId !== event.uiId).sort((a, b) => a.start - b.start);

    const dayStart = new Date(event.start).setHours(0, 0, 0, 0);
    const dayEnd = new Date(event.start).setHours(23, 59, 59, 999);
    const dayEvents = sorted.filter((e) => e.end > dayStart && e.start < dayEnd);

    let searchTime = event.start;
    let foundStart = -1;

    while (searchTime < dayEnd) {
        const overlap = dayEvents.find((e) => searchTime < e.end && searchTime + duration > e.start);
        if (overlap) {
            searchTime = overlap.end;
        } else {
            foundStart = searchTime;
            break;
        }
    }

    if (foundStart !== -1) {
        const foundEnd = foundStart + duration;
        const updatedMutation = updateMutationPosition(mutation, foundStart, foundEnd);
        return [updatedMutation];
    }
    return null;
};

const resolveTruncate = (): TimeEntryMutation[] | null => {
    const event = interaction.value.event;
    const overlaps = interaction.value.overlaps;
    const mutation = interaction.value.mutation;

    let allowedStart = event.start;
    let allowedEnd = event.end;

    const isFullyContained = overlaps.some((ov) => ov.start <= event.start && ov.end >= event.end);
    if (isFullyContained) return null;

    overlaps.forEach((ov) => {
        if (ov.start > event.start && ov.start < event.end) {
            allowedEnd = Math.min(allowedEnd, ov.start);
        }
        if (ov.end > event.start && ov.end < event.end) {
            allowedStart = Math.max(allowedStart, ov.end);
        }
    });

    if (allowedEnd <= allowedStart) return null;

    const updatedMutation = updateMutationPosition(mutation, allowedStart, allowedEnd);
    return [updatedMutation];
};

const resolveForce = (): TimeEntryMutation[] | null => {
    const event = interaction.value.event;
    const overlaps = interaction.value.overlaps;
    const mutation = interaction.value.mutation;
    const mutations: TimeEntryMutation[] = [mutation];

    for (const ov of overlaps) {
        // Completely overlapped - delete it
        if (event.start <= ov.start && event.end >= ov.end) {
            mutations.push(createDeleteMutation(ov));
            continue;
        }

        // Partially overlapped - truncate it
        if (event.start > ov.start && event.start < ov.end) {
            const updateMutation = createUpdateMutation(ov, ov.start, event.start);
            if (updateMutation) mutations.push(updateMutation);
        }

        if (event.end > ov.start && event.end < ov.end) {
            const updateMutation = createUpdateMutation(ov, event.end, ov.end);
            if (updateMutation) mutations.push(updateMutation);
        }

        // Event is fully contained within overlap - keep larger portion
        if (event.start > ov.start && event.end < ov.end) {
            const headSize = event.start - ov.start;
            const tailSize = ov.end - event.end;
            const newStart = headSize > tailSize ? ov.start : event.end;
            const newEnd = headSize > tailSize ? event.start : ov.end;
            const updateMutation = createUpdateMutation(ov, newStart, newEnd);
            if (updateMutation) mutations.push(updateMutation);
        }
    }

    return mutations;
};
</script>
