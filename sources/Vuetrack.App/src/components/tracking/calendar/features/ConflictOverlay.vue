<template>
    <BaseOverlayProvider @closed="conflict.cancel()" @submit="submit" :loading="isCommitting" :target="targetSelector" interactive>
        <template #title>
            {{ $t("calendar.conflict.title") }}
        </template>
        <template #content>
            <p class="text-medium-emphasis text-caption mb-4">
                {{ $t("calendar.conflict.hint") }}
            </p>

            <div class="d-flex flex-wrap ga-2 mb-4">
                <VChip v-if="count > 0" :prependIcon="mdiContentSaveAlertOutline" color="warning" size="small" label>
                    {{ $t("calendar.conflict.unsaved", { count: count }, count) }}
                </VChip>
                <VChip v-if="removalCount > 0" :prependIcon="mdiDelete" color="error" size="small" label>
                    {{ $t("calendar.conflict.toDelete", { count: removalCount }, removalCount) }}
                </VChip>
                <VChip v-if="hasConflicts" :prependIcon="mdiAlertCircleOutline" color="error" size="small" label>
                    {{ $t("calendar.conflict.conflictsLeft", { count: conflictPairs.length }, conflictPairs.length) }}
                </VChip>
                <VChip v-else :prependIcon="mdiCheck" color="success" size="small" label>
                    {{ $t("calendar.conflict.noConflicts") }}
                </VChip>
            </div>

            <div class="d-flex flex-column ga-2">
                <VBtn
                    v-for="strategy in strategies"
                    :key="strategy.id"
                    @click="runStrategy(strategy)"
                    :color="strategy.color"
                    :disabled="isCommitting || !selectionHasOverlaps"
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
        <template #actions>
            <VBtn @click="submit" :disabled="!canApply" :loading="isCommitting" color="primary" variant="flat">
                {{ $t("calendar.conflict.apply") }}
            </VBtn>
        </template>
    </BaseOverlayProvider>
</template>

<script setup lang="ts">
import type { ConflictTask } from "@/components/tracking/calendar/types";
import { useChangeSet } from "@/components/tracking/calendar/composables/useChangeSet";
import { useConflict } from "@/components/tracking/calendar/composables/useConflict";
import { useConflictDetection } from "@/components/tracking/calendar/composables/useConflictDetection";
import { useConflictStrategies, type ConflictResolutionStrategy } from "@/components/tracking/calendar/composables/useConflictStrategies";

const task = defineModel<ConflictTask>("task", { required: true });

const conflict = useConflict();
const notify = useNotify();

const { t } = useI18n();
const { count, removalCount, isCommitting } = useChangeSet();
const { conflictPairs, hasConflicts, getOverlapsFor } = useConflictDetection();
const { strategies } = useConflictStrategies();

const targetSelector = computed(() => "#" + task.value.event.uiId);

const canApply = computed(() => {
    if (isCommitting.value) return false;
    if (count.value === 0) return false;
    return !hasConflicts.value;
});

// A quick fix on an event that overlaps nothing has nothing to do.
const selectionHasOverlaps = computed(() => {
    const selected = conflict.selectedEvent.value;
    if (!selected) return false;

    const overlaps = getOverlapsFor(selected);
    return overlaps.length > 0;
});

const submit = () => {
    if (!canApply.value) return;
    conflict.apply();
};

const runStrategy = (strategy: ConflictResolutionStrategy) => {
    const resolved = conflict.previewStrategy(strategy.resolve);
    if (resolved) return;

    notify.warning(t("calendar.conflict.reverted"));
};
</script>
