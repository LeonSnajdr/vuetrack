import { defineConfig } from "vite";
import Vue from "@vitejs/plugin-vue";
import { fileURLToPath, URL } from "node:url";
import AutoImport from "unplugin-auto-import/vite";
import Fonts from "unplugin-fonts/vite";
import Components from "unplugin-vue-components/vite";
import { VueRouterAutoImports } from "unplugin-vue-router";
import VueRouter from "unplugin-vue-router/vite";
import Layouts from "vite-plugin-vue-layouts-next";
import Vuetify, { transformAssetUrls } from "vite-plugin-vuetify";

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [
        VueRouter({
            dts: "typings/typed-router.d.ts"
        }),
        Layouts(),
        AutoImport({
            imports: [
                "vue",
                "pinia",
                "vue-i18n",
                "@vueuse/core",
                VueRouterAutoImports,
                {
                    "@mdi/js": Object.keys(await import("@mdi/js")).filter((x) => x.startsWith("mdi")),
                    uuid: [["v4", "uuidv4"]],
                    vuetify: ["useDate"]
                }
            ],
            dirs: ["src/contracts/**", "src/stores/**", "src/services/**"],
            dts: "typings/auto-imports.d.ts",
            eslintrc: {
                enabled: true
            },
            vueTemplate: true
        }),
        Components({
            directoryAsNamespace: true,
            collapseSamePrefixes: true,
            dts: "typings/components.d.ts"
        }),
        Vue({
            template: { transformAssetUrls }
        }),
        // https://github.com/vuetifyjs/vuetify-loader/tree/master/packages/vite-plugin#readme
        Vuetify({
            autoImport: true,
            styles: {
                configFile: "src/styles/settings.scss"
            }
        }),
        Fonts({
            fontsource: {
                families: [
                    {
                        name: "Roboto",
                        weights: [100, 300, 400, 500, 700, 900],
                        styles: ["normal", "italic"]
                    }
                ]
            }
        })
    ],
    optimizeDeps: {
        exclude: ["vuetify", "vue-router", "unplugin-vue-router/runtime", "unplugin-vue-router/data-loaders", "unplugin-vue-router/data-loaders/basic"]
    },
    define: { "process.env": {} },
    resolve: {
        alias: {
            "@": fileURLToPath(new URL("src", import.meta.url))
        },
        extensions: [".js", ".json", ".jsx", ".mjs", ".ts", ".tsx", ".vue"]
    },
    server: {
        port: 3000
    }
});
