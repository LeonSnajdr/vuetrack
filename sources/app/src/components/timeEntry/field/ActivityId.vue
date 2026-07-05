<template>
    <VRowSingle>
        <VAutocomplete
            v-bind="$attrs"
            v-model.trim="activityId"
            :autoSelectFirst="!disableAutoSelectFirst"
            :items="activities"
            :label="$t('timeEntry.field.activityId')"
            :loading="isLoading"
            :rules="disableRequired ? undefined : [rules.required()]"
            itemTitle="name"
            itemValue="id"
        />
    </VRowSingle>
</template>

<script setup lang="ts">
import type { ProjectId } from "@/contracts/ProjectContract";
import type { ActivityId } from "@/contracts/ActivityContract";

const props = defineProps<{
    projectId: ProjectId | null;
    disableRequired?: boolean;
    disableAutoSelectFirst?: boolean;
}>();

const activityId = defineModel<ActivityId | null>({ required: true });

const rules = useRules();
const { data: activities, execute: loadActivities, isLoading } = useAsyncState(ProjectService.loadActivities, { initialValue: [] });

watch(
    () => props.projectId,
    () => {
        if (!props.projectId) return;
        loadActivities(props.projectId);
    },
    { immediate: true }
);

watch(
    () => props.projectId,
    () => (activityId.value = null)
);

whenever(
    () => !props.disableAutoSelectFirst && activities.value.length === 1,
    () => {
        activityId.value = activities.value[0]!.id;
    },
    { immediate: true }
);
</script>
