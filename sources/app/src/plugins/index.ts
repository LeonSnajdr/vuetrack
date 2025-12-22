import type { App } from "vue";
import pinia from "./pinia";
import router from "./router";
import vuetify from "./vuetify";
import i18n from "./i18n";

export function registerPlugins(app: App) {
    app.use(vuetify);
    app.use(router);
    app.use(pinia);
    app.use(i18n);
}
