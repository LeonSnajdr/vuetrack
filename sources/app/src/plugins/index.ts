import type { App } from "vue";
import pinia from "./pinia";
import router from "./router";
import vuetify from "./vuetify";
import i18n from "./i18n";
import { createRulesPlugin } from "vuetify/labs/rules";

export function registerPlugins(app: App) {
    app.use(vuetify);
    app.use(createRulesPlugin({}, vuetify.locale));
    app.use(router);
    app.use(pinia);
    app.use(i18n);
}
