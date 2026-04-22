<template>
    <VToolbar class="px-4" flat>
        <TrackingTimePeriodSelection />
        <div id="tracking-toolbar-prepend" class="d-inline-flex align-center ga-2 ml-2" />
        <template #append>
            <div class="d-flex align-center ga-2">
                <div id="tracking-toolbar-append" class="d-inline-flex align-center ga-2" />
                <VBtnToggle density="compact" variant="outlined">
                    <VBtn :to="{ name: 'trackingCalendar', query: $route.query }"> Calendar </VBtn>
                    <VBtn :to="{ name: 'trackingList', query: $route.query }"> List</VBtn>
                </VBtnToggle>
            </div>
        </template>
    </VToolbar>
</template>
