<template>
    <VRowSingle>
        <VAutocomplete
            v-bind="$attrs"
            v-model="projectId"
            :items="projects"
            :label="$t('timeEntry.field.projectId')"
            :rules="[rules.required()]"
            itemTitle="name"
            itemValue="id"
        />
    </VRowSingle>
</template>

<script setup lang="ts">
import type { ProjectId } from "@/contracts/ProjectContract";

const projectId = defineModel<ProjectId>({ required: true });

const rules = useRules();

const projectStore = useProjectStore();

const { projects } = storeToRefs(projectStore);
</script>
