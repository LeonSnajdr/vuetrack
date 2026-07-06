<template>
    <VMenu
        v-model="state.show"
        :closeOnContentClick="false"
        :contentProps="{ style: 'pointer-events: none' }"
        :offset="12"
        :openOnHover="false"
        :target="[state.x, state.y]"
        location="end"
        scrollStrategy="close"
    >
        <VCard v-if="timeEntry" class="pa-3" width="360">
            <div class="d-flex flex-column ga-1">
                <div class="font-weight-bold">
                    {{ timeEntry.taskId ?? timeEntry.project?.name }}
                </div>

                <div v-if="issue.isLoading.value" class="d-flex align-center ga-2 text-medium-emphasis">
                    <VProgressCircular size="16" width="2" indeterminate />
                    <span>{{ $t("calendar.event.details.loading") }}</span>
                </div>
                <template v-else-if="issue.data.value?.summary">
                    <div>{{ issue.data.value.summary }}</div>
                    <div class="v-chip-group">
                        <VChip v-if="issue.data.value.type" density="comfortable" size="small">
                            {{ issue.data.value.type }}
                        </VChip>
                        <VChip v-if="issue.data.value.status" density="comfortable" size="small">
                            {{ issue.data.value.status }}
                        </VChip>
                    </div>
                </template>
            </div>

            <div class="d-flex flex-column ga-1 mt-3 text-body-2">
                <div class="d-flex ga-3">
                    <span class="text-medium-emphasis flex-shrink-0 text-no-wrap" style="width: 84px">{{ $t("calendar.event.details.time") }}</span>
                    <span>{{ dateFormatter.format(state.event!.start, "fullTime24h") }} – {{ dateFormatter.format(state.event!.end, "fullTime24h") }}</span>
                </div>
                <div v-if="timeEntry.project?.name" class="d-flex ga-3">
                    <span class="text-medium-emphasis flex-shrink-0 text-no-wrap" style="width: 84px">{{ $t("calendar.event.details.project") }}</span>
                    <span>{{ timeEntry.project.name }}</span>
                </div>
                <div v-if="timeEntry.activity?.name" class="d-flex ga-3">
                    <span class="text-medium-emphasis flex-shrink-0 text-no-wrap" style="width: 84px">{{ $t("calendar.event.details.activity") }}</span>
                    <span>{{ timeEntry.activity.name }}</span>
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
import { useAsyncState } from "@/composables/useAsyncState";
import IssueDetailsService, { isIssueKey } from "@/services/IssueDetailsService";

const { state } = useEventDetails();

const dateFormatter = useDate();

const timeEntry = computed(() => {
    const event = state.value.event;
    if (event && (event.kind === "existing" || event.kind === "suggestion")) return event.timeEntry;
    return null;
});

const issue = useAsyncState((id: string) => IssueDetailsService.get(id));

whenever(
    () => state.value.show && state.value.event,
    () => {
        const taskId = timeEntry.value?.taskId;
        if (isIssueKey(taskId)) issue.execute(taskId!);
        else issue.data.value = null;
    }
);
</script>
