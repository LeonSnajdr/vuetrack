<template>
    <VContainer>
        <VAlert :icon="mdiLogout">{{ $t("auth.logout.loading") }}</VAlert>
    </VContainer>
</template>

<script setup lang="ts">
import AuthPlugin from "@samhammer/authentication-vue";

onBeforeMount(() => {
    logout();
});

const logout = async () => {
    const rootUrl: string = window.location.href.split("/#/")[0] + "/";
    await AuthPlugin.logout(rootUrl);
};
</script>
