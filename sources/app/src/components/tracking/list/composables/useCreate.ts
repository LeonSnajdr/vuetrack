export function useCreate() {
    const listStore = useTrackingListStore();
    const timeEntryStore = useTimeEntryStore();

    const notify = useNotify();
    const timeEntryHelper = useTimeEntry();
    const { t } = useI18n();

    const { interaction } = storeToRefs(listStore);

    const start = () => {
        interaction.value = { kind: "create", create: timeEntryHelper.createDefaultTimeEntry() };
    };

    const finish = async (createAnother: boolean) => {
        if (interaction.value.kind !== "create") return;

        const result = await timeEntryStore.create(interaction.value.create);

        if (result.status === "error") {
            notify.error(t("action.create.error", { type: t("timeEntry.singular") }), { timeout: 5000 });
        }

        if (result.status === "success" && createAnother) {
            interaction.value.create = timeEntryHelper.createDefaultTimeEntry({ startTime: interaction.value.create.endTime });
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
