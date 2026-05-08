<template>
    <VAppBar class="v-app-bar px-1 rounded-b" density="compact">
        <template #prepend>
            <VIconBtn @click="sidebarOpen = !sidebarOpen" :icon="mdiDockLeft" />
            <p class="ml-2">Vuetrack</p>
        </template>
        <template #append>
            <VIconBtn @click="settingsOpen = true" :icon="mdiCog" />
            <VIconBtn @click="logout" :icon="mdiLogout" class="ml-1" />
        </template>
    </VAppBar>
    <AppSettingsDialog v-model="settingsOpen" />
</template>

<script setup lang="ts">
const trackingStore = useTrackingStore();
const { sidebarOpen } = storeToRefs(trackingStore);

const router = useRouter();

const settingsOpen = ref(false);

const logout = () => {
    router.push({ name: "authLogout" });
};
</script>
