import vueParser from "vue-eslint-parser";
import vuePlugin from "eslint-plugin-vue";
import { defineConfigWithVueTs, vueTsConfigs as vueTsPlugin } from "@vue/eslint-config-typescript";
import vuetifyPlugin from "eslint-plugin-vuetify";
import noRelativeImportPathsPlugin from "eslint-plugin-no-relative-import-paths";
import importPlugin from "eslint-plugin-import";

export default defineConfigWithVueTs(
    vuePlugin.configs["flat/strongly-recommended"],
    vueTsPlugin.recommended,
    vuetifyPlugin.configs["flat/recommended"],
    {
        files: ["**/*.{js,mjs,cjs,ts,mts,cts,vue}"],
        plugins: {
            "no-relative-import-paths": noRelativeImportPathsPlugin,
            import: importPlugin
        },
        rules: {
            "no-relative-import-paths/no-relative-import-paths": ["warn", { allowSameFolder: true, rootDir: "src", prefix: "@" }],
            "import/first": "error",
            "import/newline-after-import": "error",
            "import/no-duplicates": "error",
            "import/no-self-import": "error"
        }
    },
    {
        files: ["**/*.vue"],
        languageOptions: {
            parser: vueParser,
            parserOptions: {
                ecmaVersion: "latest",
                sourceType: "module"
            }
        },
        plugins: {
            vue: vuePlugin,
            vuetify: vuetifyPlugin
        },
        rules: {
            "vue/html-indent": "off",
            "vue/multi-word-component-names": "off",
            "vue/v-on-event-hyphenation": ["error", "never"],
            "vue/attribute-hyphenation": ["error", "never"],
            "vue/component-name-in-template-casing": ["error", "PascalCase", { registeredComponentsOnly: false }],
            "vue/component-api-style": ["error", ["script-setup"]],
            "vue/html-self-closing": [
                "error",
                {
                    html: { void: "always", normal: "always", component: "always" },
                    svg: "always",
                    math: "always"
                }
            ],
            "vue/define-macros-order": ["error", { order: ["defineEmits", "defineProps", "defineModel"] }],
            "vue/attributes-order": [
                "error",
                {
                    order: [
                        "DEFINITION",
                        "LIST_RENDERING",
                        "CONDITIONALS",
                        "RENDER_MODIFIERS",
                        "GLOBAL",
                        ["UNIQUE", "SLOT"],
                        "TWO_WAY_BINDING",
                        "EVENTS",
                        "ATTR_DYNAMIC",
                        "ATTR_STATIC",
                        "ATTR_SHORTHAND_BOOL",
                        "OTHER_DIRECTIVES",
                        "CONTENT"
                    ],
                    alphabetical: true
                }
            ]
        }
    }
);
