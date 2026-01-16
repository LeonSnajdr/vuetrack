import type { Ref } from "vue";

export interface UseAsyncTaskOptions {
    /**
     * Callback when error is caught.
     */
    onError?: (e: unknown) => void;

    /**
     * Callback when success is caught.
     */
    onSuccess?: (data: unknown) => void;

    /**
     * Throw error when executing the execute function
     *
     * @default true
     */
    throwError?: boolean;
}

export interface UseAsyncTaskReturn<Data, Params extends unknown[]> {
    isLoading: Ref<boolean>;
    error: Ref<unknown | undefined>;
    execute: (...args: Params) => Promise<Data>;
}

/**
 * Reactive async task. Wraps an async function with loading and error state.
 *
 * @param task      The async function to be executed
 * @param options   Optional configuration
 */
export function useAsyncTask<Data, Params extends unknown[] = unknown[]>(
    task: (...args: Params) => Promise<Data>,
    options?: UseAsyncTaskOptions
): UseAsyncTaskReturn<Data, Params> {
    const { onError = globalThis.reportError ?? (() => {}), onSuccess, throwError = true } = options ?? {};

    const isLoading = shallowRef(false);
    const error = shallowRef<unknown | undefined>(undefined);

    async function execute(...args: Params): Promise<Data> {
        error.value = undefined;
        isLoading.value = true;

        try {
            const data = await task(...args);
            onSuccess?.(data);
            return data;
        } catch (e) {
            error.value = e;
            onError(e);
            if (throwError) throw e;
            return undefined as Data;
        } finally {
            isLoading.value = false;
        }
    }

    return {
        isLoading,
        error,
        execute
    };
}
