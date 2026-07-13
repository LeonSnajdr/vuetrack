import { ApiValidationException } from "@/util/ApiValidationError";

export function useCreate() {
    const listStore = useTrackingListStore();
    const timeEntryStore = useTimeEntryStore();

    const timeEntryHelper = useTimeEntryHelper();

    const { interaction } = storeToRefs(listStore);

    const start = () => {
        interaction.value = { kind: "create", create: timeEntryHelper.createDefaultTimeEntry() };
    };

    const finish = async (createAnother: boolean) => {
        if (interaction.value.kind !== "create") return;

        const result = await timeEntryStore.create(interaction.value.create);

        if (result.status === "error") {
            if (result.error instanceof ApiValidationException) interaction.value.errors = result.error.errors;
            return;
        }

        if (result.status === "success" && createAnother) {
            interaction.value.create = timeEntryHelper.createDefaultTimeEntry({ dateStarted: interaction.value.create.dateEnded });
            return;
        }

        interaction.value = { kind: "idle" };
    };

    const cancel = () => {
        if (interaction.value.kind !== "create") return;
        interaction.value = { kind: "idle" };
    };

    return { start, finish, cancel };
}
