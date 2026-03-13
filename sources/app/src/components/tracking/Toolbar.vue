<template>
    <VToolbar class="px-2" flat>
        <TrackingTimePeriodSelection />
        <template #append>
            <div class="d-flex align-center ga-2">
                <div id="tracking-toolbar-actions" class="d-inline-flex align-center ga-2" />
                <VBtnToggle density="compact" variant="outlined">
                    <VBtn :to="{ name: '/tracking/calendar' }"> Calendar </VBtn>
                    <VBtn :to="{ name: '/tracking/list' }"> List</VBtn>
                </VBtnToggle>
            </div>
        </template>
    </VToolbar>
</template>
