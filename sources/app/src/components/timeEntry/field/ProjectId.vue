<template>
    <VRowSingle>
        <VAutocomplete
            v-bind="$attrs"
            v-model="projectId"
            :autoSelectFirst="autoSelectFirstEnabled"
            :items="projects"
            :label="$t('timeEntry.field.projectId')"
            :loading="isLoading()"
            :rules="required ? [rules.required()] : undefined"
            itemTitle="name"
            itemValue="id"
        />
    </VRowSingle>
</template>

<script setup lang="ts">
import type { ProjectId } from "@/contracts/ProjectContract";

const {
    taskId,
    required = true,
    autoSelectFirst = true,
    inferFromTaskId = true
} = defineProps<{
    taskId: string | null;
    required?: boolean;
    autoSelectFirst?: boolean;
    inferFromTaskId?: boolean;
}>();

const projectId = defineModel<ProjectId | null>({ required: true });

const rules = useRules();
const projectStore = useProjectStore();
const { projects } = storeToRefs(projectStore);
const { execute: findProjectIdByTaskId, isLoading } = useAsyncTask(ProjectService.findProjectIdByTaskId);
const autoSelectFirstEnabled = computed(() => autoSelectFirst);

const updateProjectId = useDebounceFn(async () => {
    if (!inferFromTaskId) return;
    if (!taskId) return;

    const findResult = await findProjectIdByTaskId(taskId);
    if (findResult.status === "success" && findResult.data) {
        projectId.value = findResult.data;
    }
}, 500);

watch(() => taskId, updateProjectId, { immediate: true });
</script>
