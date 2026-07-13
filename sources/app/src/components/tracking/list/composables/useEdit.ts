import type { TimeEntryContract, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";
import { ApiValidationException } from "@/util/ApiValidationError";

export function useEdit() {
    const listStore = useTrackingListStore();
    const timeEntryStore = useTimeEntryStore();

    const { interaction } = storeToRefs(listStore);

    const start = (timeEntry: TimeEntryContract) => {
        interaction.value = { kind: "edit", timeEntryId: timeEntry.id, update: createEditableTimeEntry(timeEntry) };
    };

    const createEditableTimeEntry = (source: TimeEntryContract): TimeEntryUpdateContract => {
        return {
            taskId: source.taskId,
            dateStarted: source.dateStarted,
            dateEnded: source.dateEnded,
            projectId: source.project.id,
            activityId: source.activity.id,
            comment: source.comment
        };
    };

    const finish = async () => {
        if (interaction.value.kind !== "edit") return;

        const result = await timeEntryStore.update(interaction.value.timeEntryId, interaction.value.update);

        if (result.status === "success") {
            interaction.value = { kind: "idle" };
            return;
        }

        if (result.status === "error" && result.error instanceof ApiValidationException) {
            interaction.value.errors = result.error.errors;
        }
    };

    const cancel = () => {
        if (interaction.value.kind !== "edit") return;
        interaction.value = { kind: "idle" };
    };

    return { start, finish, cancel };
}
