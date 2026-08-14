import type { ValidationErrors } from "@/util/ValidationProblem";
import { type MaybeRefOrGetter } from "vue";

export function useValidation(source: MaybeRefOrGetter<ValidationErrors | undefined>) {
    const { t, te } = useI18n();

    const toKey = (error: string): string => {
        const camelError = error.charAt(0).toLowerCase() + error.slice(1);
        return `validation.${camelError}`;
    };

    const translate = (error: string): string => {
        const key = toKey(error);
        const known = te(key);
        return known ? t(key) : t("validation.invalid");
    };

    const toCamelCase = (field: string): string => {
        if (field.length === 0) return field;
        return field.charAt(0).toLowerCase() + field.slice(1);
    };

    const messages = computed<Record<string, string[]>>(() => {
        const errors = toValue(source);
        if (!errors) return {};

        const result: Record<string, string[]> = {};
        for (const [field, fieldErrors] of Object.entries(errors)) {
            result[toCamelCase(field)] = fieldErrors.map(translate);
        }

        return result;
    });

    return { messages };
}
