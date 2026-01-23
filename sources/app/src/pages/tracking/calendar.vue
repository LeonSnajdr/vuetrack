<template>
    <TrackingCalendarSidebar />
    <VContainer class="h-100 d-flex flex-column">
        <VToolbar class="px-2" flat>
            <VIconBtn :icon="mdiChevronLeft" />
            <VIconBtn :icon="mdiChevronRight" />

            <template #append>
                <VSelect :items="['Arbeitswoche', 'Woche']" variant="plain" />
                <VBtnToggle density="compact" variant="outlined">
                    <VBtn> Calendar </VBtn>
                    <VBtn> List</VBtn>
                </VBtnToggle>
            </template>
        </VToolbar>
        <TrackingCalendarView class="flex-grow-1" />
    </VContainer>
</template>

<script setup lang="ts"></script>
