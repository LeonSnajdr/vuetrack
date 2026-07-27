<template>
    <BaseOverlayProvider @closed="conflict.cancel()" @submit="submit" :loading="isCommitting" :target="targetSelector" interactive>
        <template #title>
            {{ $t("calendar.conflict.title") }}
        </template>
        <template #content>
            <p class="text-medium-emphasis text-caption mb-4">
                {{ isManual ? $t("calendar.conflict.manual.hint") : $t("calendar.conflict.subtitle", { count: task.overlaps.length }) }}
            </p>

            <div class="d-flex flex-wrap ga-2 mb-4">
                <VChip v-if="count > 0" :prependIcon="mdiContentSaveAlertOutline" color="warning" size="small" label>
                    {{ $t("calendar.conflict.manual.unsaved", { count: count }, count) }}
                </VChip>
                <VChip v-if="removalCount > 0" :prependIcon="mdiDelete" color="error" size="small" label>
                    {{ $t("calendar.conflict.manual.toDelete", { count: removalCount }, removalCount) }}
                </VChip>
                <VChip v-if="hasConflicts" :prependIcon="mdiAlertCircleOutline" color="error" size="small" label>
                    {{ $t("calendar.conflict.manual.conflictsLeft", { count: conflictPairs.length }, conflictPairs.length) }}
                </VChip>
                <VChip v-else :prependIcon="mdiCheck" color="success" size="small" label>
                    {{ $t("calendar.conflict.manual.noConflicts") }}
                </VChip>
            </div>

            <div v-if="hasConflicts" class="tc-conflict-list text-caption text-medium-emphasis mb-4">
                <div v-for="pair in conflictPairs" :key="pair.key">
                    {{ formatRange(pair.first) }} &harr; {{ formatRange(pair.second) }}
                </div>
            </div>

            <div v-if="!isManual" class="d-flex flex-column ga-2">
                <VBtn
                    v-for="strategy in strategies"
                    :key="strategy.id"
                    @click="runStrategy(strategy)"
                    :color="task.previewStrategyId === strategy.id ? 'primary' : strategy.variant"
                    :disabled="isCommitting"
                    :prependIcon="strategy.icon"
                    :variant="task.previewStrategyId === strategy.id ? 'flat' : 'tonal'"
                    class="justify-start"
                >
                    <div class="d-flex flex-column align-start text-none">
                        <span>{{ strategy.label }}</span>
                        <span class="text-caption text-medium-emphasis">{{ strategy.subtitle }}</span>
                    </div>
                </VBtn>
            </div>
        </template>
        <template #actionPrepend>
            <VBtn @click="conflict.reset()" :disabled="!canReset" :prependIcon="mdiRestore" variant="text">
                {{ $t("action.reset") }}
            </VBtn>
        </template>
        <template #actions>
            <VBtn @click="submit" :disabled="!canApply" :loading="isCommitting" color="primary" variant="flat">
                {{ $t("calendar.conflict.manual.apply") }}
            </VBtn>
        </template>
    </BaseOverlayProvider>
</template>

<script setup lang="ts">
import type { ConflictTask, TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useChangeSet } from "@/components/tracking/calendar/composables/useChangeSet";
import { useConflict } from "@/components/tracking/calendar/composables/useConflict";
import { useConflictDetection } from "@/components/tracking/calendar/composables/useConflictDetection";
import { useConflictStrategies, type ConflictResolutionStrategy } from "@/components/tracking/calendar/composables/useConflictStrategies";

const task = defineModel<ConflictTask>("task", { required: true });

const conflict = useConflict();
const notify = useNotify();
const dateFormatter = useDate();

const { t } = useI18n();
const { count, removalCount, isCommitting, has } = useChangeSet();
const { conflictPairs, hasConflicts } = useConflictDetection();
const { strategies } = useConflictStrategies();

const isManual = computed(() => task.value.mode === "manual");

const targetSelector = computed(() => "#" + task.value.event.uiId);

// The change that caused the conflict is never reset, so anything on top of it
// is what Reset undoes.
const canReset = computed(() => {
    if (isCommitting.value) return false;
    if (count.value === 0) return false;
    if (count.value === 1) return !has(task.value.event.uiId);
    return true;
});

const canApply = computed(() => {
    if (isCommitting.value) return false;
    if (count.value === 0) return false;
    return !hasConflicts.value;
});

const formatRange = (event: TimeEntryEvent): string => {
    const from = dateFormatter.format(event.start, "fullTime24h");
    const to = dateFormatter.format(event.end, "fullTime24h");
    return `${from} - ${to}`;
};

const submit = () => {
    if (!canApply.value) return;
    conflict.apply();
};

const runStrategy = (strategy: ConflictResolutionStrategy) => {
    if (strategy.id === "manual") {
        conflict.enterManual();
        return;
    }

    const resolved = conflict.previewStrategy(strategy.id, strategy.resolve);
    if (resolved) return;

    notify.warning(t("calendar.conflict.reverted"));
};
</script>

<style scoped>
.tc-conflict-list {
    max-height: 96px;
    overflow-y: auto;
}
</style>
