import Login from "@/pages/auth/login.vue";
import Logout from "@/pages/auth/logout.vue";
import Tracking from "@/pages/tracking.vue";
import Calendar from "@/pages/tracking/calendar.vue";
import List from "@/pages/tracking/list.vue";
import AuthPlugin from "@samhammer/authentication-vue";
import { createRouter, createWebHashHistory } from "vue-router";

const router = createRouter({
    history: createWebHashHistory(import.meta.env.BASE_URL),
    routes: [
        { path: "/", redirect: { name: "trackingCalendar" } },
        {
            path: "/auth",
            name: "auth",
            children: [
                {
                    path: "login",
                    name: "authLogin",
                    component: Login
                },
                {
                    path: "logout",
                    name: "authLogout",
                    component: Logout
                }
            ]
        },
        {
            path: "/tracking",
            name: "tracking",
            redirect: { name: "trackingCalendar" },
            component: Tracking,
            children: [
                {
                    path: "calendar",
                    name: "trackingCalendar",
                    component: Calendar
                },
                {
                    path: "list",
                    name: "trackingList",
                    component: List
                }
            ]
        }
    ]
});

router.beforeEach((to, _from, next) => {
    if (to.name === "authLogin") {
        if (AuthPlugin.authenticated) {
            return next({ name: "tracking" });
        }

        return next();
    }

    if (to.name === "authLogout") {
        return next();
    }

    if (!AuthPlugin.authenticated) {
        return next({ name: "authLogin", query: { returnPath: to.fullPath } });
    }

    return next();
});

export default router;
