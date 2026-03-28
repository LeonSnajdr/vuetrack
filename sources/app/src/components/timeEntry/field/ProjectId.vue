<template>
    <VRowSingle>
        <VAutocomplete
            v-bind="$attrs"
            v-model="projectId"
            :items="projects"
            :label="$t('timeEntry.field.projectId')"
            :loading="isLoading()"
            :rules="[rules.required()]"
            itemTitle="name"
            itemValue="id"
        />
    </VRowSingle>
</template>

<script setup lang="ts">
import type { ProjectId } from "@/contracts/ProjectContract";

const props = defineProps<{
    taskId: string;
}>();

const projectId = defineModel<ProjectId>({ required: true });

const rules = useRules();
const projectStore = useProjectStore();
const { projects } = storeToRefs(projectStore);
const { execute: findProjectIdByTaskId, isLoading } = useAsyncTask(ProjectService.findProjectIdByTaskId);

const updateProjectId = useDebounceFn(async () => {
    const findResult = await findProjectIdByTaskId(props.taskId);
    if (findResult.status === "success" && findResult.data) {
        projectId.value = findResult.data;
    }
}, 500);

watch(() => props.taskId, updateProjectId, { immediate: true });
</script>
