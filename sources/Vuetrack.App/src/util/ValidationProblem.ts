import axios from "axios";

export type ValidationError = "Required" | "DateOrder" | "Invalid";

export type ValidationErrors = Record<string, ValidationError[]>;

export function tryGetValidationErrors(error: unknown): ValidationErrors | null {
    if (!axios.isAxiosError(error)) return null;
    if (error.response?.status !== 400) return null;

    const data = error.response?.data;
    if (data === null || typeof data !== "object") return null;

    const errors = (data as { errors?: unknown }).errors;
    if (!isFlatStringArrayRecord(errors)) return null;

    return errors;
}

const isFlatStringArrayRecord = (value: unknown): value is ValidationErrors => {
    if (value === null || typeof value !== "object" || Array.isArray(value)) {
        return false;
    }

    return Object.values(value).every((entry) => Array.isArray(entry) && entry.every((message) => typeof message === "string"));
};
