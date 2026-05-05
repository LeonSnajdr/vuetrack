import axios from "axios";

export type ApiValidationError = Record<string, string[]>;

export class ApiValidationException extends Error {
    public readonly errors: ApiValidationError;

    constructor(errors: ApiValidationError) {
        super("Api validation error");
        this.name = "ApiValidationException";
        this.errors = errors;
    }
}

export function tryGetApiValidationError(error: unknown): ApiValidationError | null {
    if (!axios.isAxiosError(error)) return null;

    const data = error.response?.data;

    if (!isFlatStringArrayRecord(data)) return null;

    return data;
}

function isFlatStringArrayRecord(value: unknown): value is Record<string, string[]> {
    if (value === null || typeof value !== "object" || Array.isArray(value)) {
        return false;
    }

    return Object.values(value).every((entry) => Array.isArray(entry) && entry.every((message) => typeof message === "string"));
}
