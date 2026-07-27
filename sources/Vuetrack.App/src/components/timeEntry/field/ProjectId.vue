<template>
    <VRowSingle>
        <VAutocomplete
            v-bind="$attrs"
            v-model="projectId"
            v-model:menu="menuModel"
            v-model:search="search"
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

const search = ref<string>();
const menu = ref(false);
const suppressMenu = ref(false);

const menuModel = computed({
    get: () => menu.value,
    set: (value) => {
        if (value && suppressMenu.value) return;
        menu.value = value;
    }
});

const updateProjectId = useDebounceFn(async () => {
    if (props.disableInferFromTaskId) return;
    if (projectId.value) return;
    if (!props.taskId) return;

    const capturedTaskId = props.taskId;
    const findResult = await findProjectByTaskId(props.taskId);
    if (props.taskId !== capturedTaskId) return;
    if (findResult.status !== "success" || !findResult.data) return;

    suppressMenu.value = true;
    projectId.value = findResult.data.id;
    search.value = findResult.data.name;
    await nextTick();
    suppressMenu.value = false;
}, 500);

watch(() => props.taskId, updateProjectId, { immediate: true });
</script>
