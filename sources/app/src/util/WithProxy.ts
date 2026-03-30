/* eslint-disable @typescript-eslint/no-explicit-any */
export function withProxy<T extends object>(obj: T) {
    type Builder<Acc> = {
        from<S extends object, K extends keyof S & string>(source: S, ...keys: K[]): Builder<Acc & Pick<S, K>>;
        build(): Acc;
    };

    const builder: Builder<T> = {
        from(source, ...keys) {
            for (const key of keys) {
                Object.defineProperty(obj, key, {
                    get: () => source[key],
                    set: (value) => {
                        source[key] = value;
                    },
                    enumerable: true
                });
            }
            return builder as Builder<any>;
        },
        build() {
            return obj as any;
        }
    };

    return builder;
}
