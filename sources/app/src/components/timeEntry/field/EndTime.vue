<template>
    <VRowSingle>
        <BaseDateTimeInput
            v-if="granularMode"
            v-model="endTime"
            v-bind="$attrs"
            :disabled="!startTime"
            :label="$t('timeEntry.field.endTime')"
            :min="startTime"
            :rules="granularEndTimeRules"
        >
            <template #time-append-inner>
                <VIcon @click="granularMode = false" :icon="mdiMetronome" tabindex="-1" />
            </template>
        </BaseDateTimeInput>
        <VNumberInput
            v-else
            v-model="durationMinutes"
            v-bind="$attrs"
            @keydown.tab="durationMenu = false"
            :disabled="!startTime"
            :label="$t('timeEntry.field.endTime.duration')"
            :max="600"
            :min="1"
            :precision="0"
            :rules="[rules.required()]"
        >
            <VMenu v-model="durationMenu" activator="parent">
                <VSheet class="pa-4 d-flex ga-2">
                    <template v-for="minutes in [15, 30, 45, 60, 90, 120]" :key="minutes">
                        <VBtn @click="durationMinutes = minutes" density="compact" tabindex="-1" variant="tonal">{{ minutes }}</VBtn>
                    </template>
                </VSheet>
            </VMenu>
            <template #append-inner>
                <VIcon @click="granularMode = true" :icon="mdiClockDigital" tabindex="-1" />
            </template>
        </VNumberInput>
    </VRowSingle>
</template>

<script setup lang="ts">
const props = defineProps<{
    startTime: Date | null;
}>();

const endTime = defineModel<Date | null>({ required: true });

const rules = useRules();

const granularMode = ref(false);
const durationMenu = ref(false);

const granularEndTimeRules = computed(() => [rules.required(), ...(props.startTime ? [rules.dateAfter(props.startTime)] : [])]);

const durationMinutes = computed<number | null>({
    get: () => {
        if (!(props.startTime instanceof Date) || !(endTime.value instanceof Date)) {
            return null;
        }

        return Math.round((endTime.value.getTime() - props.startTime.getTime()) / (1000 * 60));
    },
    set: (value) => {
        if (!(props.startTime instanceof Date) || value === null || !Number.isFinite(value)) {
            endTime.value = null;
            return;
        }

        const nextEndTime = new Date(props.startTime);
        nextEndTime.setMinutes(nextEndTime.getMinutes() + Math.round(value));
        endTime.value = nextEndTime;
    }
});

watch(
    () => props.startTime,
    () => {
        if (endTime.value === null) return;
        if (props.startTime === null || props.startTime >= endTime.value) {
            endTime.value = null;
        }
    }
);
</script>
