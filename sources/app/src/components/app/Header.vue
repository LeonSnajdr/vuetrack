<template>
    <VAppBar class="v-app-bar px-1" density="compact">
        <template #prepend>
            <VIconBtn :icon="mdiDockLeft" />
            <p class="ml-2">Vuetrack</p>
        </template>
        <template #append>
            <VIconBtn @click="logout" :icon="mdiLogout" />
        </template>
    </VAppBar>
</template>

<script setup lang="ts">
const router = useRouter();

const logout = () => {
    router.push({ name: "authLogout" });
};
</script>
