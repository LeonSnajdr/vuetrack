import type { App } from "vue";
import pinia from "./pinia";
import router from "./router";
import vuetify from "./vuetify";
import i18n from "./i18n";
import { createRulesPlugin } from "vuetify/labs/rules";

export function registerPlugins(app: App) {
    app.use(vuetify);
    app.use(
        createRulesPlugin(
            {
                aliases: {
                    dateAfter: (startTime: Date | null | undefined, err?: string) => {
                        return (value: Date | null | undefined) => {
                            if (!(startTime instanceof Date) || Number.isNaN(startTime.getTime())) {
                                return true;
                            }

                            if (!(value instanceof Date) || Number.isNaN(value.getTime())) {
                                return true;
                            }

                            return value.getTime() > startTime.getTime() || err || i18n.global.t("$vuetify.rules.dateAfter");
                        };
                    }
                }
            },
            vuetify.locale
        )
    );
    app.use(router);
    app.use(pinia);
    app.use(i18n);
}
