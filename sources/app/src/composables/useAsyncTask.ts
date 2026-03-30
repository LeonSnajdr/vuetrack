import type { ActionResult } from "@/util/ActionResult";
import axios from "axios";

export type AsyncTaskFn<TArgs extends unknown[], TResult> = ((...args: TArgs) => Promise<TResult>) | ((...args: [...TArgs, AbortSignal]) => Promise<TResult>);

export interface AsyncTaskKeyContext<TArgs extends unknown[]> {
    args: TArgs;
}

export type CancelPolicy = "none" | "previous" | "byKey";

export interface UseAsyncTaskConfig<TArgs extends unknown[], TKey> {
    cancelPolicy?: CancelPolicy;
    key?: (ctx: AsyncTaskKeyContext<TArgs>) => TKey;
}

export function useAsyncTask<TArgs extends unknown[], TResult, TKey = symbol>(fn: AsyncTaskFn<TArgs, TResult>, config: UseAsyncTaskConfig<TArgs, TKey> = {}) {
    const { cancelPolicy = "none", key } = config;
    const canCancel = cancelPolicy !== "none";

    const abortControllers = new Map<TKey | symbol, AbortController>();
    const loadingCounts = reactive(new Map<TKey | symbol, number>());

    const getKey = (args: TArgs): TKey | symbol => {
        if (key) {
            return key({ args });
        }

        return Symbol("global");
    };

    const increaseLoading = (key: TKey | symbol): void => {
        loadingCounts.set(key, (loadingCounts.get(key) ?? 0) + 1);
    };

    const decreaseLoading = (key: TKey | symbol): void => {
        const current = loadingCounts.get(key) ?? 0;

        if (current <= 1) {
            loadingCounts.delete(key);
            return;
        }

        loadingCounts.set(key, current - 1);
    };

    const isLoading = computed(() => (key?: TKey | symbol): boolean => {
        if (key !== undefined) {
            return (loadingCounts.get(key) ?? 0) > 0;
        }

        return Array.from(loadingCounts.values()).some((count) => count > 0);
    });

    const execute = async (...args: TArgs): Promise<ActionResult<TResult>> => {
        const key = getKey(args);

        if (canCancel) {
            abortControllers.get(key)?.abort();
        }

        const controller = canCancel ? new AbortController() : null;

        if (controller) {
            abortControllers.set(key, controller);
        }

        increaseLoading(key);

        try {
            const result = await (fn as (...a: [...TArgs, AbortSignal]) => Promise<TResult>)(...args, controller?.signal as AbortSignal);

            return success(result);
        } catch (e) {
            if (isCancelledError(e)) {
                return cancelled();
            }

            console.error(e);
            return error();
        } finally {
            if (controller && abortControllers.get(key) === controller) {
                abortControllers.delete(key);
            }

            decreaseLoading(key);
        }
    };

    const cancel = (key: TKey | symbol): void => {
        if (!canCancel) return;

        abortControllers.get(key)?.abort();
        abortControllers.delete(key);
    };

    const isCancelledError = (e: unknown): boolean => axios.isCancel(e) || (e instanceof DOMException && e.name === "AbortError");

    return {
        execute,
        cancel,
        isLoading,
        isCancelledError
    };
}
