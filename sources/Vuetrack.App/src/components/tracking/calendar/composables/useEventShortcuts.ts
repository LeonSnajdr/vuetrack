import type { TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useCreate } from "./useCreate";
import { useEdit } from "./useEdit";
import { useDelete } from "./useDelete";
import { useCalendarTimePeriod } from "./useCalendarTimePeriod";
import { useEventContextMenu } from "./useEventContextMenu";
import { useEventPolicy } from "./useEventPolicy";
import { useEventSelection } from "./useEventSelection";
import { useStagedRemoval } from "./useStagedRemoval";

export function useEventShortcuts() {
    const create = useCreate();
    const edit = useEdit();
    const remove = useDelete();
    const { isReadonly } = useCalendarTimePeriod();
    const { selectedEvent, clearSelection } = useEventSelection();
    const contextMenu = useEventContextMenu();
    const policy = useEventPolicy();
    const stagedRemoval = useStagedRemoval();

    const getShortcutTarget = (): TimeEntryEvent | null => {
        if (isReadonly.value) return null;

        const event = selectedEvent.value;
        if (!event) return null;
        if (event.kind === "draft") return null;

        return event;
    };

    const getTaskTarget = (): TimeEntryEvent | null => {
        if (!policy.canOpenTask()) return null;
        return getShortcutTarget();
    };

    const startEdit = () => {
        const event = getTaskTarget();
        if (!event) return;
        if (event.kind !== "existing" && event.kind !== "suggestion") return;

        contextMenu.close();
        edit.start(event);
    };

    const startAccept = () => {
        const event = getTaskTarget();
        if (!event) return;
        if (event.kind !== "suggestion") return;

        contextMenu.close();
        create.start(event);
    };

    // While resolving a conflict manually the same key stages and unstages a
    // removal instead of opening the delete dialog.
    const startDelete = () => {
        const event = getShortcutTarget();
        if (!event) return;

        if (policy.canStageRemoval(event)) {
            contextMenu.close();
            stagedRemoval.toggle(event);
            return;
        }

        if (!policy.canOpenTask()) return;

        contextMenu.close();
        remove.start(event);
    };

    // Overlays bind escape themselves, so it only drops the selection while none
    // is open.
    const dropSelection = () => {
        if (!policy.canOpenTask()) return;
        clearSelection();
    };

    useHotkey("e", startEdit);
    useHotkey("a", startAccept);
    useHotkey("delete/d", startDelete);
    useHotkey("escape", dropSelection);
}
