<template>
    <VRowSingle>
        <VAutocomplete
            v-bind="$attrs"
            v-model="projectId"
            :autoSelectFirst="!disableAutoSelectFirst"
            :items="projects"
            :label="$t('timeEntry.field.projectId')"
            :loading="isLoading"
            :rules="disableRequired ? undefined : [rules.required()]"
            itemTitle="name"
            itemValue="id"
        />
    </VRowSingle>
</template>

<script setup lang="ts">
import type { ProjectId } from "@/contracts/ProjectContract";

const props = defineProps<{
    taskId: string | null;
    disableRequired?: boolean;
    disableAutoSelectFirst?: boolean;
    disableInferFromTaskId?: boolean;
}>();

const projectId = defineModel<ProjectId | null>({ required: true });

const projectStore = useProjectStore();
const { projects } = storeToRefs(projectStore);

const rules = useRules();
const { execute: findProjectByTaskId, isLoading } = useAsyncTask(ProjectService.findProjectByTaskId);

const updateProjectId = useDebounceFn(async () => {
    if (props.disableInferFromTaskId) return;
    if (!props.taskId) return;

    const findResult = await findProjectByTaskId(props.taskId);
    if (findResult.status === "success" && findResult.data) {
        projectId.value = findResult.data.id;
    }
}, 500);

watch(() => props.taskId, updateProjectId);
</script>
