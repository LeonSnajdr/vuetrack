<template>
    <VToolbar class="px-2" flat>
        <VIconBtn :icon="mdiChevronLeft" />
        <VIconBtn :icon="mdiChevronRight" />

        <template #append>
            <VBtnToggle density="compact" variant="outlined">
                <VBtn :to="{ name: '/tracking/calendar' }"> Calendar </VBtn>
                <VBtn :to="{ name: '/tracking/list' }"> List</VBtn>
            </VBtnToggle>
        </template>
    </VToolbar>
</template>
