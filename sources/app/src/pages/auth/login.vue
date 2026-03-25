<template>
    <div>Login...</div>
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
            const returnPath = route.query.returnPath;
            console.debug("login successful navigating to", returnPath);

            router.push({ path: returnPath as string });
        } else {
            console.debug("dogin login");
            await AuthPlugin.login("keycloak");
        }
    } catch (error) {
        console.error("login error", error);
    }
};
</script>
