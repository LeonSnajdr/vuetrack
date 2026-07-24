import { createI18n } from "vue-i18n";
import translations from "@/translations";

function getBrowserLanguage(): string {
    return window.navigator.language ? window.navigator.language.substring(0, 2) : "";
}

export default createI18n({
    locale: getBrowserLanguage(),
    fallbackLocale: import.meta.env.VITE_DEFAULT_LANGUAGE,
    legacy: false,
    warnHtmlMessage: false,
    messages: translations
});
