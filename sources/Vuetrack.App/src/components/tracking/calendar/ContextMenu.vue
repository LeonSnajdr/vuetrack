<template>
    <VMenu v-model="contextMenu.show" :target="[contextMenu.x, contextMenu.y]">
        <VList v-if="contextMenu.event" density="compact">
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
            <VListItem @click="runContextAction(remove.start)" :prependIcon="mdiDelete" :title="$t('action.delete')">
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

const calendarStore = useCalendarStore();
const { interaction } = storeToRefs(calendarStore);

const create = useCreate();
const edit = useEdit();
const remove = useDelete();
const { setContextMenuOpen } = useEventDetails();
const { state: contextMenu, close } = useEventContextMenu();

watch(
    () => contextMenu.value.show,
    (show) => setContextMenuOpen(show)
);

watch(
    () => interaction.value.kind !== "idle",
    (active) => {
        if (active) close();
    }
);

const runContextAction = (action: (event: ContextMenuEvent) => void) => {
    if (!contextMenu.value.event) return;
    action(contextMenu.value.event);
    close();
};
</script>
