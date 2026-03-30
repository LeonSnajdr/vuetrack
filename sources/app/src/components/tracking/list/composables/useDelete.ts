import type { TimeEntryId } from "@/contracts/TimeEntryContract";
import type { ActionResult } from "@/util/ActionResult";
import { cancelled } from "@/util/ActionResult";

export function useDelete() {
    const listStore = useTrackingListStore();
    const timeEntryStore = useTimeEntryStore();
    const notify = useNotify();
    const { t } = useI18n();

    const { interaction } = storeToRefs(listStore);

    const start = (timeEntryId: TimeEntryId) => {
        interaction.value = { kind: "delete", timeEntryId };
    };

    const finish = async (): Promise<ActionResult> => {
        if (interaction.value.kind !== "delete") return cancelled();

        const result = await timeEntryStore.remove(interaction.value.timeEntryId);

        if (result.status === "error") {
            notify.error(t("action.delete.error", { type: t("timeEntry.singular") }), { timeout: 5000 });
            return result;
        }

        interaction.value = { kind: "idle" };
        return result;
    };

    const cancel = () => {
        if (interaction.value.kind !== "delete") return;
        interaction.value = { kind: "idle" };
    };

    return { start, finish, cancel };
}
