<template>
    <VMenu v-model="contextMenu.show" :target="[contextMenu.x, contextMenu.y]">
        <VList v-if="contextMenu.event" density="compact">
            <template v-if="!isManualConflict">
                <VListItem @click="runContextAction(edit.start)" :prependIcon="mdiPencil" :title="$t('action.edit')">
                    <template #append>
                        <VHotkey class="ml-10" keys="e" />
                    </template>
                </VListItem>
                <VListItem
                    v-if="contextMenu.event.kind === 'suggestion'"
                    @click="runContextAction((e) => e.kind === 'suggestion' && create.start(e))"
                    :prependIcon="mdiCheck"
                    :title="$t('action.accept')"
                >
                    <template #append>
                        <VHotkey class="ml-10" keys="a" />
                    </template>
                </VListItem>
            </template>
            <VListItem @click="runContextAction(runDeleteAction)" :prependIcon="deleteIcon" :title="deleteTitle">
                <template #append>
                    <VHotkey class="ml-10" keys="delete/d" />
                </template>
            </VListItem>
        </VList>
    </VMenu>
</template>

<script setup lang="ts">
import { useCreate } from "./composables/useCreate";
import { useEdit } from "./composables/useEdit";
import { useDelete } from "./composables/useDelete";
import { useEventDetails } from "./composables/useEventDetails";
import { useEventContextMenu, type ContextMenuEvent } from "./composables/useEventContextMenu";
import { useEventPolicy } from "./composables/useEventPolicy";
import { useStagedRemoval } from "./composables/useStagedRemoval";

const calendarStore = useCalendarStore();
const { gesture, task } = storeToRefs(calendarStore);

const { t } = useI18n();

const create = useCreate();
const edit = useEdit();
const remove = useDelete();
const stagedRemoval = useStagedRemoval();
const { isManualConflict, canStageRemoval } = useEventPolicy();
const { setContextMenuOpen } = useEventDetails();
const { state: contextMenu, close } = useEventContextMenu();

const isRestorable = computed(() => {
    const event = contextMenu.value.event;
    if (!event) return false;
    return stagedRemoval.isStaged(event);
});

const deleteIcon = computed(() => (isRestorable.value ? mdiDeleteRestore : mdiDelete));
const deleteTitle = computed(() => (isRestorable.value ? t("action.restore") : t("action.delete")));

watch(
    () => contextMenu.value.show,
    (show) => setContextMenuOpen(show)
);

// The menu belongs to the state it was opened in, so any transition closes it.
watch([() => gesture.value.kind, () => task.value.kind], () => close());

const runDeleteAction = (event: ContextMenuEvent) => {
    if (canStageRemoval(event) || stagedRemoval.isStaged(event)) {
        stagedRemoval.toggle(event);
        return;
    }

    remove.start(event);
};

const runContextAction = (action: (event: ContextMenuEvent) => void) => {
    if (!contextMenu.value.event) return;
    action(contextMenu.value.event);
    close();
};
</script>
