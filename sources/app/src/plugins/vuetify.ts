import "vuetify/styles";
import "@/styles/v-show.scss";
import "@/styles/v-field.scss";
import "@/styles/v-input.scss";
import "@/styles/v-chip.scss";
import "@mdi/font/css/materialdesignicons.css";
import { mdi, aliases } from "vuetify/iconsets/mdi-svg";
import { createVuetify } from "vuetify";
import { de, en } from "vuetify/locale";

import i18n from "./i18n";
import dateFns from "./dateFns";

import { VIconBtn } from "vuetify/labs/VIconBtn";
import { VDateInput } from "vuetify/labs/VDateInput";

export default createVuetify({
    components: {
        VIconBtn,
        VDateInput
    },
    date: {
        adapter: dateFns
    },
    locale: {
        messages: { de: de, en: en },
        locale: i18n.global.locale.value
    },
    icons: {
        defaultSet: "mdi",
        aliases: aliases,
        sets: {
            mdi
        }
    },
    theme: {
        defaultTheme: "system",
        // https://github.com/vuetifyjs/vuetify/blob/master/packages/vuetify/src/composables/theme.ts#L128
        themes: {
            light: {
                colors: {
                    primary: "#303A4E",
                    "primary-lighten-1": "#636B7A",
                    "primary-lighten-2": "#969BA5",
                    "primary-lighten-3": "#E4E5E8",
                    secondary: "#8098A7",
                    "secondary-lighten-1": "#9FB1BD",
                    "secondary-lighten-2": "#BFCBD2",
                    "secondary-lighten-3": "#DEE4E8",
                    "secondary-lighten-4": "#F2F5F6",
                    tertiary: "#599B8B",
                    "tertiary-lighten-1": "#82B4A8",
                    "tertiary-lighten-2": "#ABCCC4",
                    "tertiary-lighten-3": "#D4E5E1",
                    background: "#F9FAFC",
                    surface: "#FFFFFF",
                    "on-surface": "#1C1C1C",
                    error: "#E52727",
                    "error-lighten-1": "#DCBABA",
                    warning: "#DC8B22",
                    "warning-lighten-1": "#E2D4C1",
                    success: "#7D9E39",
                    "success-lighten-1": "#D7E1C2",
                    info: "#2196F3"
                },
                variables: {
                    "border-color": "#000000",
                    "high-emphasis-opacity": "1",
                    "medium-emphasis-opacity": 0.73,
                    "disabled-opacity": 0.51
                }
            },
            dark: {
                colors: {
                    primary: "#4271C4",
                    "primary-lighten-1": "#345898",
                    "primary-lighten-2": "#26406C",
                    "primary-lighten-3": "#182840",
                    secondary: "#8098A7",
                    "secondary-lighten-1": "#627682",
                    "secondary-lighten-2": "#45545E",
                    "secondary-lighten-3": "#273239",
                    "secondary-lighten-4": "#22282B",
                    tertiary: "#599B8B",
                    "tertiary-lighten-1": "#45786D",
                    "tertiary-lighten-2": "#31564F",
                    "tertiary-lighten-3": "#1E3332",
                    background: "#121212",
                    surface: "#1b1b1b",
                    "surface-light": "#1b1b1b",
                    "on-surface": "#FFFFFF",
                    error: "#E52727",
                    "error-lighten-1": "#DCBABA",
                    warning: "#DC8B22",
                    "warning-lighten-1": "#E2D4C1",
                    success: "#C9F56F",
                    "success-lighten-1": "#D7E1C2",
                    info: "#2196F3"
                }
            }
        }
    },
    defaults: {
        VSnackbar: {
            timeout: 5000,
            color: "info",
            location: "bottom right",
            class: "text-break"
        },
        VBanner: {
            style: "flex: unset",
            rounded: true,
            border: true,
            elevation: 1
        },
        VSwitch: {
            color: "primary",
            hideDetails: "auto"
        },
        VTextField: {
            variant: "outlined",
            hideDetails: "auto",
            autocomplete: "off",
            persistentPlaceholder: true,
            VIcon: { size: "small" }
        },
        VDateInput: {
            variant: "outlined",
            hideDetails: "auto",
            prependIcon: "",
            autocomplete: "off",
            persistentPlaceholder: true,
            VIcon: { size: "small" }
        },
        VSelect: {
            variant: "outlined",
            hideDetails: "auto",
            autocomplete: "off",
            persistentPlaceholder: true,
            VIcon: { size: "small" }
        },
        VCombobox: {
            variant: "outlined",
            hideDetails: "auto",
            autocomplete: "off",
            persistentPlaceholder: true,
            VIcon: { size: "small" }
        },
        VAutocomplete: {
            variant: "outlined",
            autoSelectFirst: true,
            hideDetails: "auto",
            autocomplete: "off",
            persistentPlaceholder: true,
            VIcon: { size: "small" }
        },
        VNumberInput: {
            variant: "outlined",
            persistentPlaceholder: true,
            hideDetails: "auto",
            autocomplete: "off"
        },
        VTextarea: {
            variant: "outlined",
            hideDetails: "auto",
            autocomplete: "off",
            persistentPlaceholder: true
        },
        VCheckbox: {
            hideDetails: "auto"
        },
        VField: {
            variant: "outlined"
        },
        VTable: {
            VPagination: {
                density: "comfortable",
                showFirstLastPage: true
            }
        },
        VDataTable: {
            density: "comfortable"
        },
        VDataTableServer: {
            density: "comfortable"
        },
        VChip: {
            color: "primary",
            variant: "flat"
        },
        VCard: {
            class: "d-flex flex-column",

            VCardTitle: {
                class: "text-title-large d-flex align-center"
            }
        },
        VNavigationDrawer: {
            class: "elevation-1",
            railWidth: "72",
            width: "210",
            rounded: true,
            retainFocus: true,
            VListItem: {
                rounded: true,
                class: "mx-3 px-3 my-1",
                VListItemTitle: {
                    class: "text-caption"
                }
            }
        },
        VProgressCircular: {
            size: 20
        },
        VProgressLinear: {
            height: 2,
            color: "secondary"
        },
        VTabs: {
            density: "compact",
            color: "primary",

            VTab: {
                class: "font-weight-bold px-2"
            }
        },
        VExpansionPanel: {
            VExpansionPanelText: {
                class: "pt-2"
            }
        },
        VDialog: {
            transition: false,
            retainFocus: true,
            VCardActions: {
                class: "mx-4"
            }
        },
        VMenu: {
            transition: false,
            retainFocus: true
        },
        VTooltip: {
            openDelay: 400
        },
        VIconBtn: {
            rounded: "lg",
            variant: "text",
            sizes: [
                ["x-small", 24],
                ["small", 28],
                ["default", 32],
                ["large", 36],
                ["x-large", 40]
            ],
            iconSizes: [
                ["x-small", 12],
                ["small", 16],
                ["default", 20],
                ["large", 24],
                ["x-large", 28]
            ]
        }
    }
});
