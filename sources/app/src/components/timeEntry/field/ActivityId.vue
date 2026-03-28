<template>
    <VRowSingle>
        <VAutocomplete
            v-bind="$attrs"
            v-model="activityId"
            :items="activities"
            :label="$t('timeEntry.field.activityId')"
            :loading="isLoading"
            :rules="[rules.required()]"
            itemTitle="name"
            itemValue="id"
        />
    </VRowSingle>
</template>

<script setup lang="ts">
import type { ProjectId } from "@/contracts/ProjectContract";
import type { ActivityId } from "@/contracts/ActivityContract";

const props = defineProps<{
    projectId: ProjectId;
}>();

const activityId = defineModel<ActivityId>({ required: true });

const rules = useRules();

const { data: activities, execute: loadActivities, isLoading } = useAsyncState(ProjectService.loadActivities, { initialValue: [] });

watch(
    () => props.projectId,
    () => loadActivities(props.projectId),
    { immediate: true }
);
</script>
