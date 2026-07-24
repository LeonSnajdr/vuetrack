<template>
    <VMenu
        v-model="state.show"
        :closeOnContentClick="false"
        :contentProps="{ style: `pointer-events: ${state.pinned ? 'auto' : 'none'}` }"
        :offset="12"
        :openOnHover="false"
        :target="[state.x, state.y]"
        location="end"
        scrollStrategy="close"
    >
        <VCard v-if="timeEntry" ref="cardRef" class="pa-3" width="360">
            <div class="d-flex flex-column ga-1">
                <div class="d-flex align-center ga-2 font-weight-bold">
                    <span class="text-truncate">{{ displayTitle }}</span>
                    <VSpacer />
                    <VHotkey class="mr-n2" keys="shift" />
                    <VIcon :icon="state.pinned ? mdiPinOff : mdiPin" size="small" />
                </div>

                <div v-if="details.isLoading.value" class="d-flex align-center ga-2 text-medium-emphasis">
                    <VProgressCircular size="16" width="2" indeterminate />
                    <span>{{ $t("calendar.event.details.loading") }}</span>
                </div>
                <template v-for="(group, groupIndex) in detailGroups" v-else :key="`${group.connectorKey}-${groupIndex}`">
                    <div v-if="group.chips.length" class="v-chip-group">
                        <VChip v-for="(chip, chipIndex) in group.chips" :key="chipIndex" density="comfortable" size="small">
                            {{ chip.value }}
                        </VChip>
                    </div>
                    <div v-for="(row, rowIndex) in group.rows" :key="rowIndex" class="d-flex ga-3">
                        <span class="text-medium-emphasis flex-shrink-0 text-no-wrap" style="width: 84px">{{ $t(`detail.field.${row.label}`) }}</span>
                        <span v-if="row.kind === 'text'">{{ row.value }}</span>
                        <span v-else-if="row.kind === 'date'">{{ dateFormatter.format(row.value, "fullDate") }}</span>
                        <a v-else-if="row.kind === 'link'" :href="row.url" class="d-inline-flex align-center ga-1" rel="noopener" target="_blank">
                            {{ row.text }}
                            <VIcon :icon="mdiOpenInNew" size="14" />
                        </a>
                    </div>
                </template>
            </div>

            <div class="d-flex flex-column ga-1 mt-3 text-body-2">
                <div class="d-flex ga-3">
                    <span class="text-medium-emphasis flex-shrink-0 text-no-wrap" style="width: 84px">{{ $t("calendar.event.details.time") }}</span>
                    <span>{{ dateFormatter.format(state.event!.start, "fullTime24h") }} – {{ dateFormatter.format(state.event!.end, "fullTime24h") }}</span>
                </div>
                <div v-if="projectName" class="d-flex ga-3">
                    <span class="text-medium-emphasis flex-shrink-0 text-no-wrap" style="width: 84px">{{ $t("calendar.event.details.project") }}</span>
                    <span>{{ projectName }}</span>
                </div>
                <div v-if="activityName" class="d-flex ga-3">
                    <span class="text-medium-emphasis flex-shrink-0 text-no-wrap" style="width: 84px">{{ $t("calendar.event.details.activity") }}</span>
                    <span>{{ activityName }}</span>
                </div>
                <div v-if="timeEntry.comment" class="d-flex ga-3">
                    <span class="text-medium-emphasis flex-shrink-0 text-no-wrap" style="width: 84px">{{ $t("calendar.event.details.comment") }}</span>
                    <span>{{ timeEntry.comment }}</span>
                </div>
            </div>
        </VCard>
    </VMenu>
</template>

<script setup lang="ts">
import { useEventDetails } from "./composables/useEventDetails";
import type { ChipDetailField, DetailField } from "@/contracts/DetailsContract";
import DetailsService from "@/services/DetailsService";

type DetailFieldGroup = {
    connectorKey: string;
    chips: ChipDetailField[];
    rows: Exclude<DetailField, ChipDetailField>[];
};

const { state, togglePin, hardClose } = useEventDetails();

const calendarStore = useCalendarStore();
const { interaction } = storeToRefs(calendarStore);

const dateFormatter = useDate();

const cardRef = useTemplateRef("cardRef");

onKeyStroke("Shift", (nativeEvent) => {
    if (nativeEvent.repeat || !state.value.show) return;
    nativeEvent.preventDefault();
    togglePin();
});

onClickOutside(cardRef, () => {
    if (state.value.show) hardClose();
});

watch(
    () => interaction.value.kind !== "idle",
    (active) => {
        if (active) hardClose();
    }
);

const timeEntry = computed(() => {
    const event = state.value.event;
    if (event && (event.kind === "existing" || event.kind === "suggestion")) return event.timeEntry;
    return null;
});

const displayTitle = computed(() => {
    const event = state.value.event;
    if (event?.kind === "existing") return event.timeEntry.taskId ?? event.timeEntry.project.name;
    if (event?.kind === "suggestion") return event.timeEntry.taskId ?? event.timeEntry.projectName;
    return null;
});

const projectName = computed(() => {
    const event = state.value.event;
    if (event?.kind === "existing") return event.timeEntry.project.name;
    if (event?.kind === "suggestion") return event.timeEntry.projectName;
    return null;
});
const activityName = computed(() => (state.value.event?.kind === "existing" ? state.value.event.timeEntry.activity.name : null));

const details = useAsyncState((taskId: string) => DetailsService.get(taskId));

const detailGroups = computed<DetailFieldGroup[]>(() => {
    const groups = details.data.value?.groups ?? [];
    return groups.map((group) => ({
        connectorKey: group.connectorKey,
        chips: group.fields.filter((field): field is ChipDetailField => field.kind === "chip"),
        rows: group.fields.filter((field): field is Exclude<DetailField, ChipDetailField> => field.kind !== "chip")
    }));
});

whenever(
    () => state.value.show && state.value.event,
    () => {
        const taskId = timeEntry.value?.taskId;
        if (taskId) details.execute(taskId);
        else details.data.value = null;
    }
);
</script>
