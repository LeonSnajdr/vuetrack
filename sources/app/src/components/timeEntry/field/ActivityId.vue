<template>
    <VRowSingle>
        <VAutocomplete
            v-bind="$attrs"
            v-model="activityId"
            :autoSelectFirst="autoSelectFirstEnabled"
            :items="activities"
            :label="$t('timeEntry.field.activityId')"
            :loading="isLoading"
            :rules="required ? [rules.required()] : undefined"
            itemTitle="name"
            itemValue="id"
        />
    </VRowSingle>
</template>

<script setup lang="ts">
import type { ProjectId } from "@/contracts/ProjectContract";
import type { ActivityId } from "@/contracts/ActivityContract";

const {
    projectId,
    required = true,
    autoSelectFirst = true
} = defineProps<{
    projectId: ProjectId | null;
    required?: boolean;
    autoSelectFirst?: boolean;
}>();

const activityId = defineModel<ActivityId | null>({ required: true });

const rules = useRules();
const { data: activities, execute: loadActivities, isLoading } = useAsyncState(ProjectService.loadActivities, { initialValue: [] });
const autoSelectFirstEnabled = computed(() => autoSelectFirst);

watch(
    () => projectId,
    () => {
        if (!projectId) return;
        loadActivities(projectId);
    },
    { immediate: true }
);

watch(
    () => projectId,
    () => (activityId.value = null)
);

whenever(
    () => autoSelectFirst && activities.value.length === 1,
    () => {
        activityId.value = activities.value[0]!.id;
    },
    { immediate: true }
);
</script>
