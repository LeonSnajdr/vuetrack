<template>
    <VMenu :closeOnContentClick="false" location="bottom end">
        <template #activator="{ props }">
            <VBtn v-bind="props" :appendIcon="mdiChevronDown" :prependIcon="mdiCalendarWeekBegin" variant="text">
                {{ selectedWeekdaysLabel }}
            </VBtn>
        </template>
        <VCard minWidth="280">
            <VCardText class="d-flex flex-wrap ga-2 pb-2">
                <VBtn @click="setSelectedWeekdays([todayWeekday])" size="small" variant="tonal">{{ $t("calendar.weekdaySelection.today") }}</VBtn>
                <VBtn @click="setSelectedWeekdays([1, 2, 3, 4, 5])" size="small" variant="tonal">{{ $t("calendar.weekdaySelection.workweek") }}</VBtn>
                <VBtn @click="setSelectedWeekdays([1, 2, 3, 4, 5, 6, 0])" size="small" variant="tonal">{{ $t("calendar.weekdaySelection.fullWeek") }}</VBtn>
            </VCardText>
            <VDivider />
            <VList density="compact">
                <VListItem v-for="weekday in weekdayOptions" :key="weekday.value" @click="toggleWeekday(weekday.value)">
                    <template #prepend>
                        <VCheckboxBtn :modelValue="selectedWeekdays.includes(weekday.value)" />
                    </template>
                    <VListItemTitle>{{ weekday.label }}</VListItemTitle>
                </VListItem>
            </VList>
        </VCard>
    </VMenu>
</template>

<script setup lang="ts">
const selectedWeekdays = defineModel<number[]>({ required: true });

const { locale, t } = useI18n();

const weekDayOrder = [1, 2, 3, 4, 5, 6, 0];
const today = new Date();
const todayWeekday = today.getDay() === 0 ? 0 : today.getDay();
const weekdayFormatter = computed(() => new Intl.DateTimeFormat(locale.value, { weekday: "short" }));

const weekdayOptions = computed(() => {
    const monday = getMondayOfCurrentWeek();

    return weekDayOrder.map((weekday) => {
        const date = new Date(monday);
        date.setDate(monday.getDate() + getWeekdayOffset(weekday));

        return {
            value: weekday,
            label: weekdayFormatter.value.format(date)
        };
    });
});

const selectedWeekdaysLabel = computed(() => {
    if (selectedWeekdays.value.length === 1 && selectedWeekdays.value[0] === todayWeekday) return t("calendar.weekdaySelection.today");
    if (selectedWeekdays.value.length === 7) return t("calendar.weekdaySelection.fullWeek");
    if (selectedWeekdays.value.length === 5 && weekDayOrder.slice(0, 5).every((day) => selectedWeekdays.value.includes(day))) return t("calendar.weekdaySelection.workweek");

    return weekdayOptions.value
        .filter((weekday) => selectedWeekdays.value.includes(weekday.value))
        .map((weekday) => weekday.label)
        .join(", ");
});

const getMondayOfCurrentWeek = () => {
    const now = new Date();
    const daysSinceMonday = (now.getDay() + 6) % 7;
    const monday = new Date(now);
    monday.setDate(now.getDate() - daysSinceMonday);
    monday.setHours(0, 0, 0, 0);
    return monday;
};

const getWeekdayOffset = (weekday: number) => {
    return weekday === 0 ? 6 : weekday - 1;
};

const setSelectedWeekdays = (weekdays: number[]) => {
    selectedWeekdays.value = [...weekDayOrder.filter((weekday) => weekdays.includes(weekday))];
};

const toggleWeekday = (weekday: number) => {
    if (selectedWeekdays.value.includes(weekday)) {
        if (selectedWeekdays.value.length === 1) return;
        selectedWeekdays.value = selectedWeekdays.value.filter((selectedWeekday) => selectedWeekday !== weekday);
        return;
    }

    selectedWeekdays.value = [...selectedWeekdays.value, weekday].sort((left, right) => weekDayOrder.indexOf(left) - weekDayOrder.indexOf(right));
};
</script>
