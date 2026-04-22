export type Nullable<T> = {
    [K in keyof T]: T[K] | null;
};

export function isNonNullable<T>(value: Nullable<T>): value is T {
    // TODO implement a real impl
    return true;
}
