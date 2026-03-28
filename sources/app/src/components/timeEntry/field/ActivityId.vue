<template>
    <VRowSingle>
        <VNumberInput v-bind="$attrs" v-model="activityId" :label="$t('timeEntry.field.activityId')" :rules="[rules.required()]" />
    </VRowSingle>
</template>

<script setup lang="ts">
import type { ActivityId } from "@/contracts/TimeEntryContract";

const activityId = defineModel<ActivityId>({ required: true });

const rules = useRules();
</script>
