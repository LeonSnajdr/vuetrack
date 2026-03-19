import Tracking from "@/pages/tracking.vue";
import Calendar from "@/pages/tracking/calendar.vue";
import List from "@/pages/tracking/list.vue";
import { createRouter, createWebHashHistory } from "vue-router";

const router = createRouter({
    history: createWebHashHistory(import.meta.env.BASE_URL),
    routes: [
        { path: "/", redirect: { name: "trackingCalendar" } },
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

export default router;
