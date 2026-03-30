<template>
    <VAppBar class="v-app-bar px-1" density="compact">
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
