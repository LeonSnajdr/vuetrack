<template>
    <BaseOverlayProvider @closed="conflict.cancel()" :loading="conflictLoadingId != null" :target="targetSelector">
        <template #title>
            {{ $t("calendar.conflict.title") }}
        </template>
        <template #content>
            <p class="text-medium-emphasis text-caption mb-4">
                {{ $t("calendar.conflict.subtitle", { count: interaction.overlaps.length }) }}
            </p>
            <div class="d-flex flex-column ga-2">
                <VBtn
                    v-for="strategy in defaultStrategies"
                    :key="strategy.id"
                    @click="executeStrategy(strategy)"
                    :color="strategy.variant"
                    :disabled="conflictLoadingId !== null"
                    :loading="conflictLoadingId === strategy.id"
                    :prependIcon="strategy.icon"
                    class="justify-start"
                    variant="tonal"
                >
                    <div class="d-flex flex-column align-start text-none">
                        <span>{{ strategy.label }}</span>
                        <span class="text-caption text-medium-emphasis">{{ strategy.subtitle }}</span>
                    </div>
                </VBtn>
            </div>
        </template>
    </BaseOverlayProvider>
</template>

<script setup lang="ts">
import type { Interaction, TimeEntryEvent, TimeEntryMutation } from "@/components/tracking/calendar/types";
import { useCalendarHelper } from "@/components/tracking/calendar/composables/useCalendarHelper";
import { useConflict } from "@/components/tracking/calendar/composables/useConflict";
import { useEventWrapper } from "@/components/tracking/calendar/composables/useEventWrapper";

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
const { draftEvents, existingEvents } = storeToRefs(calendarStore);
const notify = useNotify();

const { t } = useI18n();
const { startOfDay, addDays } = useDateHelper();
const { applyEventPosition, buildCreateMutation, buildDeleteMutation, buildUpdateMutation, withMutationPosition } = useCalendarHelper();
const { cloneEventAsDraft } = useEventWrapper();

const conflictLoadingId = ref<string | null>(null);

const targetSelector = computed(() => "#" + interaction.value.event.uiId);

const buildOverlapUpdateMutation = (event: TimeEntryEvent, newStart: number, newEnd: number): TimeEntryMutation | null => {
    if (event.kind !== "existing" && event.kind !== "suggestion") return null;
    const base = buildUpdateMutation(event, { start: event.start, end: event.end });
    return withMutationPosition(base, newStart, newEnd);
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
        notify.warning(t("calendar.conflict.reverted"));
    }
};

const getSearchWindow = (event: TimeEntryEvent) => {
    const windowStart = startOfDay(new Date(event.start)).getTime();
    const lastOccupiedMs = Math.max(event.start, event.end - 1);
    const windowEndExclusive = addDays(startOfDay(new Date(lastOccupiedMs)), 1).getTime();

    return { windowStart, windowEndExclusive };
};

const getSearchCandidates = (event: TimeEntryEvent) => {
    const { windowStart, windowEndExclusive } = getSearchWindow(event);
    const candidates = [...existingEvents.value]
        .filter((candidate) => candidate.uiId !== event.uiId)
        .sort((a, b) => a.start - b.start)
        .filter((candidate) => candidate.end > windowStart && candidate.start < windowEndExclusive);

    return { candidates, windowStart, windowEndExclusive };
};

// Default strategies
const resolveShiftUp = (): TimeEntryMutation[] | null => {
    const event = interaction.value.event;
    const mutation = interaction.value.mutation;
    const duration = event.end - event.start;
    const { candidates, windowStart, windowEndExclusive } = getSearchCandidates(event);

    let searchTime = event.start;

    while (searchTime >= windowStart) {
        const potentialStart = searchTime;
        const potentialEnd = searchTime + duration;

        if (potentialEnd > windowEndExclusive) {
            searchTime = windowEndExclusive - duration;
            continue;
        }

        const overlap = candidates.find((candidate) => potentialStart < candidate.end && potentialEnd > candidate.start);
        if (overlap) {
            searchTime = overlap.start - duration;
        } else {
            const updatedMutation = withMutationPosition(mutation, potentialStart, potentialEnd);
            applyEventPosition(event, potentialStart, potentialEnd);
            return [updatedMutation];
        }
    }

    return null;
};

const resolveShiftDown = (): TimeEntryMutation[] | null => {
    const event = interaction.value.event;
    const mutation = interaction.value.mutation;
    const duration = event.end - event.start;
    const { candidates, windowEndExclusive } = getSearchCandidates(event);

    let searchTime = event.start;

    while (searchTime + duration <= windowEndExclusive) {
        const overlap = candidates.find((candidate) => searchTime < candidate.end && searchTime + duration > candidate.start);
        if (overlap) {
            searchTime = overlap.end;
        } else {
            const foundEnd = searchTime + duration;
            const updatedMutation = withMutationPosition(mutation, searchTime, foundEnd);
            applyEventPosition(event, searchTime, foundEnd);
            return [updatedMutation];
        }
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

    const updatedMutation = withMutationPosition(mutation, allowedStart, allowedEnd);
    applyEventPosition(event, allowedStart, allowedEnd);
    return [updatedMutation];
};

const resolveForce = (): TimeEntryMutation[] | null => {
    const event = interaction.value.event;
    const overlaps = interaction.value.overlaps;
    const mutation = interaction.value.mutation;
    const deletes: TimeEntryMutation[] = [];
    const updates: TimeEntryMutation[] = [];
    const creates: TimeEntryMutation[] = [];

    const pushOverlapUpdate = (ov: TimeEntryEvent, newStart: number, newEnd: number) => {
        const updateMutation = buildOverlapUpdateMutation(ov, newStart, newEnd);
        if (!updateMutation) return;
        updates.push(updateMutation);
        applyEventPosition(ov, newStart, newEnd);
    };

    const pushOverlapSplit = (ov: TimeEntryEvent, headEnd: number, tailStart: number): boolean => {
        if (ov.kind !== "existing" && ov.kind !== "suggestion") return false;
        const tailEnd = ov.end;
        pushOverlapUpdate(ov, ov.start, headEnd);
        const tailEvent = cloneEventAsDraft(ov, tailStart, tailEnd);
        draftEvents.value.push(tailEvent);
        creates.push(buildCreateMutation(tailEvent));
        return true;
    };

    for (const ov of overlaps) {
        // Completely overlapped - delete it
        if (event.start <= ov.start && event.end >= ov.end) {
            deletes.push(buildDeleteMutation(ov));
            continue;
        }

        // Event is fully contained within overlap - split it so the new
        // event fits in the middle (head update + tail create).
        if (event.start > ov.start && event.end < ov.end) {
            if (pushOverlapSplit(ov, event.start, event.end)) continue;
        }

        // Partially overlapped - truncate it
        if (event.start > ov.start && event.start < ov.end) {
            pushOverlapUpdate(ov, ov.start, event.start);
        }

        if (event.end > ov.start && event.end < ov.end) {
            pushOverlapUpdate(ov, event.end, ov.end);
        }
    }

    // Order matters: deletes free space, head shrinks make room for the
    // primary, then tail creates fill in below — so the backend never sees
    // overlapping ranges mid-operation.
    return [...deletes, ...updates, mutation, ...creates];
};
</script>
