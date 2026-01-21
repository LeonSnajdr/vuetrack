export function withProxy<T extends object, S, K extends keyof S>(obj: T, source: S, ...keys: K[]): T & Pick<S, K> {
    for (const key of keys) {
        Object.defineProperty(obj, key, {
            get: () => source[key],
            set: (value) => {
                source[key] = value;
            },
            enumerable: true
        });
    }
    return obj as T & Pick<S, K>;
}
