<template>
    <VContainer class="h-100">
        <VEmptyState :text="$t('auth.logout.loading')">
            <template #media>
                <BaseMascotLoader />
            </template>
        </VEmptyState>
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
