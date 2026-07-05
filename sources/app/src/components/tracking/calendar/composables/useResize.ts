import type { TimeEntryEvent } from "@/components/tracking/calendar/types";
import { useCalendarHelper } from "./useCalendarHelper";
import { useEventMutation } from "./useEventMutation";

export function useResize() {
    const calendarStore = useCalendarStore();
    const { interaction, existingEvents } = storeToRefs(calendarStore);
    const mutation = useEventMutation();
    const { roundTime, getEventBoundaries, prepareUpdateMutation, updateEventPosition, restoreOriginalPosition } = useCalendarHelper();

    const isCommitting = ref(false);

    const start = (event: TimeEntryEvent) => {
        const resizeMutation = prepareUpdateMutation(event);
        if (!resizeMutation) return;

        interaction.value = {
            kind: "resize",
            event: resizeMutation.event,
            mutation: resizeMutation
        };
    };

    const update = (mouseMs: number) => {
        if (isCommitting.value) return;
        if (interaction.value.kind !== "resize") return;

        const { event } = interaction.value;
        const snapPoints = getEventBoundaries(event, existingEvents.value);
        const mouseRounded = roundTime(mouseMs, { down: false, snapPoints });

        updateEventPosition(event, { end: mouseRounded }, "start");
    };

    const finish = async () => {
        if (interaction.value.kind !== "resize") return;

        isCommitting.value = true;
        const shouldIdle = await mutation.commitUpdate(interaction.value);
        isCommitting.value = false;
        if (shouldIdle) {
            interaction.value = { kind: "idle" };
        }
    };

    const cancel = () => {
        if (interaction.value.kind !== "resize") return;
        restoreOriginalPosition(interaction.value.mutation);
        interaction.value = { kind: "idle" };
    };

    return { start, update, finish, cancel };
}
