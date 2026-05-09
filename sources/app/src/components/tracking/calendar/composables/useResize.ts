import type { TimeEntryEvent } from "@/components/tracking/calendar/types";
import { ApiValidationException } from "@/util/ApiValidationError";
import { useCalendarHelper } from "./useCalendarHelper";
import { useEventMutation } from "./useEventMutation";

export function useResize() {
    const calendarStore = useCalendarStore();
    const { interaction, existingEvents } = storeToRefs(calendarStore);
    const mutation = useEventMutation();
    const {
        roundTime,
        cancelPendingUpdateForEvent,
        getOriginalPosition,
        getEventBoundaries,
        buildUpdateMutation,
        updateEventPosition,
        restoreOriginalPosition
    } = useCalendarHelper();

    const start = (event: TimeEntryEvent) => {
        if (event.kind !== "existing" && event.kind !== "suggestion") return;

        cancelPendingUpdateForEvent(event);

        const originalPosition = getOriginalPosition(event, interaction.value);
        const resizeMutation = buildUpdateMutation(event, originalPosition);

        interaction.value = {
            kind: "resize",
            event,
            mutation: resizeMutation
        };
    };

    const update = (mouseMs: number) => {
        if (interaction.value.kind !== "resize") return;

        const { event } = interaction.value;
        const snapPoints = getEventBoundaries(event, existingEvents.value);
        const mouseRounded = roundTime(mouseMs, { down: false, snapPoints });

        updateEventPosition(event, { end: mouseRounded }, "start");
    };

    const finish = async () => {
        if (interaction.value.kind !== "resize") return;
        const cur = interaction.value;

        if (cur.event.kind === "existing" && mutation.tryEnterConflict(cur.event, cur.mutation)) return;

        const resizeResult = await mutation.execute(cur.mutation);

        if (resizeResult.status === "error" && resizeResult.error instanceof ApiValidationException) {
            interaction.value = {
                kind: "edit",
                event: cur.event,
                mutation: cur.mutation,
                errors: resizeResult.error.errors
            };
            return;
        }

        if (resizeResult.status !== "success") {
            restoreOriginalPosition(cur.mutation);
        }

        interaction.value = { kind: "idle" };
    };

    const cancel = () => {
        if (interaction.value.kind !== "resize") return;

        restoreOriginalPosition(interaction.value.mutation);

        interaction.value = { kind: "idle" };
    };

    return { start, update, finish, cancel };
}
