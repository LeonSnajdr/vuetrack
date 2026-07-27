import type { TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useCreate } from "./useCreate";
import { useEdit } from "./useEdit";
import { useDelete } from "./useDelete";
import { useCalendarTimePeriod } from "./useCalendarTimePeriod";
import { useEventHover } from "./useEventHover";
import { useEventContextMenu } from "./useEventContextMenu";
import { useEventPolicy } from "./useEventPolicy";
import { useStagedRemoval } from "./useStagedRemoval";

export function useEventShortcuts() {
    const create = useCreate();
    const edit = useEdit();
    const remove = useDelete();
    const { isReadonly } = useCalendarTimePeriod();
    const { hoveredEvent } = useEventHover();
    const contextMenu = useEventContextMenu();
    const policy = useEventPolicy();
    const stagedRemoval = useStagedRemoval();

    const getTargetEvent = (): TimeEntryEvent | null => {
        if (contextMenu.state.value.show) return contextMenu.state.value.event;
        return hoveredEvent.value;
    };

    const getShortcutTarget = (): TimeEntryEvent | null => {
        if (isReadonly.value) return null;

        const event = getTargetEvent();
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

    useHotkey("e", startEdit);
    useHotkey("a", startAccept);
    useHotkey("delete/d", startDelete);
}
