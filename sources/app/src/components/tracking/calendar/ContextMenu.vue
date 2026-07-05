<template>
    <VMenu v-model="contextMenu.show" :target="[contextMenu.x, contextMenu.y]">
        <VList v-if="contextMenu.event" density="compact">
            <VListItem @click="runContextAction(edit.start)" :prependIcon="mdiPencil" :title="$t('action.edit')">
                <template #append>
                    <VHotkey class="ml-2" keys="ctrl" />
                    <VIcon :icon="mdiMouseRightClickOutline" size="small" />
                </template>
            </VListItem>
            <VListItem
                v-if="contextMenu.event.kind === 'suggestion'"
                @click="runContextAction((e) => e.kind === 'suggestion' && create.start(e))"
                :prependIcon="mdiCheck"
                :title="$t('action.accept')"
            />
            <VListItem @click="runContextAction(remove.start)" :prependIcon="mdiDelete" :title="$t('action.delete')" />
        </VList>
    </VMenu>
</template>

<script setup lang="ts">
import type { CalendarEvent } from "vuetify/lib/components/VCalendar/types.mjs";
import type { ExistingTimeEntryEvent, SuggestionTimeEntryEvent } from "./types";
import { useCreate } from "./composables/useCreate";
import { useEdit } from "./composables/useEdit";
import { useDelete } from "./composables/useDelete";
import { useCalendarTimePeriod } from "./composables/useCalendarTimePeriod";

type ContextMenuEvent = ExistingTimeEntryEvent | SuggestionTimeEntryEvent;

const calendarStore = useCalendarStore();
const { interaction } = storeToRefs(calendarStore);

const create = useCreate();
const edit = useEdit();
const remove = useDelete();
const { isReadonly } = useCalendarTimePeriod();

const contextMenu = ref<{ show: boolean; x: number; y: number; event: ContextMenuEvent | null }>({
    show: false,
    x: 0,
    y: 0,
    event: null
});

const canStartInteraction = (currentKind: string): boolean => {
    return currentKind !== "create" && currentKind !== "edit" && currentKind !== "conflict" && currentKind !== "delete";
};

const open = (nativeEvent: Event, event?: CalendarEvent) => {
    const mouseEvent = nativeEvent as MouseEvent;
    mouseEvent.preventDefault();
    if (isReadonly.value) return;
    if (!event) return;
    if (!canStartInteraction(interaction.value.kind)) return;
    if (event.kind !== "existing" && event.kind !== "suggestion") return;

    const target = event as ContextMenuEvent;

    // Ctrl (Strg) + right click is a shortcut that opens the edit directly.
    if (mouseEvent.ctrlKey) {
        edit.start(target);
        return;
    }

    contextMenu.value = { show: true, x: mouseEvent.clientX, y: mouseEvent.clientY, event: target };
};

const runContextAction = (action: (event: ContextMenuEvent) => void) => {
    if (!contextMenu.value.event) return;
    action(contextMenu.value.event);
    contextMenu.value.show = false;
};

defineExpose({ open });
</script>
