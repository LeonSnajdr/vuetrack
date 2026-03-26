<template>
    <VToolbar class="px-2" flat>
        <TrackingTimePeriodSelection />
        <div id="tracking-toolbar-prepend" class="d-inline-flex align-center ga-2 ml-2" />
        <template #append>
            <div class="d-flex align-center ga-2">
                <div id="tracking-toolbar-append" class="d-inline-flex align-center ga-2" />
                <VBtnToggle density="compact" variant="outlined">
                    <VBtn :to="{ name: 'trackingCalendar' }"> Calendar </VBtn>
                    <VBtn :to="{ name: 'trackingList' }"> List</VBtn>
                </VBtnToggle>
            </div>
        </template>
    </VToolbar>
</template>
