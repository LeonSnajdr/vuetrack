import AuthPlugin from "@samhammer/authentication-vue";
import { createRouter, createWebHashHistory } from "vue-router";
import { routes } from "vue-router/auto-routes";

const router = createRouter({
    history: createWebHashHistory(import.meta.env.BASE_URL),
    routes: [{ path: "/", redirect: "/tracking/calendar" }, ...routes]
});

router.beforeEach((to) => {
    if (to.name === "/auth/login") {
        if (AuthPlugin.authenticated) {
            return { name: "/tracking" };
        }

        return true;
    }

    if (to.name === "/auth/logout") {
        return true;
    }

    if (!AuthPlugin.authenticated) {
        return { name: "/auth/login", query: { returnPath: encodeURIComponent(to.fullPath) } };
    }

    return true;
});

export default router;
