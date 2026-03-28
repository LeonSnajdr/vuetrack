<template>
    <VRowSingle>
        <VAutocomplete
            v-bind="$attrs"
            v-model="activityId"
            :items="availableActivities"
            :label="$t('timeEntry.field.activityId')"
            :rules="[rules.required()]"
            itemTitle="name"
            itemValue="id"
        />
    </VRowSingle>
</template>

<script setup lang="ts">
import type { ActivityId } from "@/contracts/ActivityContract";

const activityId = defineModel<ActivityId>({ required: true });

const rules = useRules();

const availableActivities = ref([
    {
        id: 4,
        name: "Activity"
    }
]);
</script>
