<template>
    <VContainer>
        <VAlert :icon="mdiLogin">{{ $t("auth.login.loading") }}</VAlert>
    </VContainer>
</template>

<script setup lang="ts">
import AuthPlugin from "@samhammer/authentication-vue";

const route = useRoute();
const router = useRouter();

onBeforeMount(() => {
    login();
});

const login = async () => {
    try {
        if (AuthPlugin.authenticated) {
            const returnPath = decodeURIComponent(route.query.returnPath as string);
            console.debug("login successful navigating to", returnPath);
            router.push(returnPath);
        } else {
            console.debug("dogin login");
            await AuthPlugin.login("keycloak");
        }
    } catch (error) {
        console.error("login error", error);
    }
};
</script>
