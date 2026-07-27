import { canStartInteraction, type TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useCreate } from "./useCreate";
import { useEdit } from "./useEdit";
import { useDelete } from "./useDelete";
import { useCalendarTimePeriod } from "./useCalendarTimePeriod";
import { useEventHover } from "./useEventHover";
import { useEventContextMenu } from "./useEventContextMenu";

export function useEventShortcuts() {
    const calendarStore = useCalendarStore();
    const { interaction } = storeToRefs(calendarStore);

    const create = useCreate();
    const edit = useEdit();
    const remove = useDelete();
    const { isReadonly } = useCalendarTimePeriod();
    const { hoveredEvent } = useEventHover();
    const contextMenu = useEventContextMenu();

    const getTargetEvent = (): TimeEntryEvent | null => {
        if (contextMenu.state.value.show) return contextMenu.state.value.event;
        return hoveredEvent.value;
    };

    const getShortcutTarget = (): TimeEntryEvent | null => {
        if (isReadonly.value) return null;
        if (!canStartInteraction(interaction.value.kind)) return null;

        const event = getTargetEvent();
        if (!event) return null;
        if (event.kind === "draft") return null;

        return event;
    };

    const startEdit = () => {
        const event = getShortcutTarget();
        if (!event) return;
        if (event.kind !== "existing" && event.kind !== "suggestion") return;

        contextMenu.close();
        edit.start(event);
    };

    const startAccept = () => {
        const event = getShortcutTarget();
        if (!event) return;
        if (event.kind !== "suggestion") return;

        contextMenu.close();
        create.start(event);
    };

    const startDelete = () => {
        const event = getShortcutTarget();
        if (!event) return;

        contextMenu.close();
        remove.start(event);
    };

    useHotkey("e", startEdit);
    useHotkey("a", startAccept);
    useHotkey("delete/d", startDelete);
}
