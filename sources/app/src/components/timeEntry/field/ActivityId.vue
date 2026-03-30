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
            autoSelectFirst
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
    () => activities.value.length === 1,
    () => {
        activityId.value = activities.value[0]!.id;
    },
    { immediate: true }
);
</script>
