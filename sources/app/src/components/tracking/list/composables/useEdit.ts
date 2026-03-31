import type { TimeEntryContract } from "@/contracts/TimeEntryContract";

export function useEdit() {
    const listStore = useTrackingListStore();
    const timeEntryStore = useTimeEntryStore();
    const notify = useNotify();
    const { t } = useI18n();

    const { interaction } = storeToRefs(listStore);

    const start = (timeEntry: TimeEntryContract) => {
        interaction.value = { kind: "edit", timeEntryId: timeEntry.id, update: createEditableTimeEntry(timeEntry) };
    };

    const createEditableTimeEntry = (source: TimeEntryContract): TimeEntryUpdateContract => {
        return {
            taskId: source.taskId,
            startTime: source.startTime,
            endTime: source.endTime,
            projectId: source.project.id,
            activityId: source.activity.id,
            comment: source.comment
        };
    };

    const finish = async () => {
        if (interaction.value.kind !== "edit") return;

        const result = await timeEntryStore.update(interaction.value.timeEntryId, interaction.value.update);

        if (result.status === "error") {
            notify.error(t("action.save.error", { type: t("timeEntry.singular") }), { timeout: 5000 });
        }

        interaction.value = { kind: "idle" };
    };

    const cancel = () => {
        if (interaction.value.kind !== "edit") return;
        interaction.value = { kind: "idle" };
    };

    return { start, finish, cancel };
}
