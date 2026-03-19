<template>
    <VToolbar class="px-2" flat>
        <TrackingTimePeriodSelection />
        <template #append>
            <div class="d-flex align-center ga-2">
                <div id="tracking-toolbar-actions" class="d-inline-flex align-center ga-2" />
                <VBtnToggle density="compact" variant="outlined">
                    <VBtn :to="{ name: 'trackingCalendar' }"> Calendar </VBtn>
                    <VBtn :to="{ name: 'trackingList' }"> List</VBtn>
                </VBtnToggle>
            </div>
        </template>
    </VToolbar>
</template>
