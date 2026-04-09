export type Nullable<T> = {
    [K in keyof T]: T[K] | null;
};

export function isNonNullable<T>(value: Nullable<T>): value is T {
    return hasNoNullishValues(value);
}

function hasNoNullishValues(value: unknown): boolean {
    if (value === null || value === undefined) {
        return false;
    }

    if (value instanceof Date) {
        return !Number.isNaN(value.getTime());
    }

    if (Array.isArray(value)) {
        return value.every((item) => hasNoNullishValues(item));
    }

    if (typeof value !== "object") {
        return true;
    }

    return Object.values(value as Record<string, unknown>).every((item) => hasNoNullishValues(item));
}
