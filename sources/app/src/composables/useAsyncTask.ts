import type { ActionResult } from "@/util/ActionResult";
import axios from "axios";

export type AsyncTaskFn<TArgs extends unknown[], TResult> = ((...args: TArgs) => Promise<TResult>) | ((...args: [...TArgs, AbortSignal]) => Promise<TResult>);

export interface AsyncTaskKeyContext<TArgs extends unknown[]> {
    args: TArgs;
}

export type CancelPolicy<TKey, TArgs extends unknown[]> = "none" | "previous" | ((ctx: AsyncTaskKeyContext<TArgs>) => TKey);

export interface UseAsyncTaskConfig<TArgs extends unknown[], TKey> {
    cancelPolicy?: CancelPolicy<TKey, TArgs>;
    supportsAbort?: boolean;
}

export function useAsyncTask<TArgs extends unknown[], TResult, TKey = symbol>(fn: AsyncTaskFn<TArgs, TResult>, config: UseAsyncTaskConfig<TArgs, TKey> = {}) {
    const { cancelPolicy = "previous", supportsAbort = true } = config;

    const globalKey = Symbol("global");
    const pending = new Map<TKey | symbol, AbortController>();

    const canCancel = supportsAbort && cancelPolicy !== "none";

    const getKey = (args: TArgs): TKey | symbol => {
        if (typeof cancelPolicy === "function") {
            return cancelPolicy({ args });
        }
        return globalKey;
    };

    const execute = async (...args: TArgs): Promise<ActionResult<TResult>> => {
        const key = getKey(args);

        if (canCancel) {
            pending.get(key)?.abort();
        }

        const controller = canCancel ? new AbortController() : null;

        if (controller) {
            pending.set(key, controller);
        }

        try {
            const result = supportsAbort
                ? await (fn as (...a: [...TArgs, AbortSignal]) => Promise<TResult>)(...args, controller!.signal)
                : await (fn as (...a: TArgs) => Promise<TResult>)(...args);

            return success(result);
        } catch (e) {
            if (supportsAbort && isCancelledError(e)) {
                return cancelled();
            }
            console.error(e);
            return error();
        } finally {
            if (controller && pending.get(key) === controller) {
                pending.delete(key);
            }
        }
    };

    const cancel = (...args: TArgs): void => {
        if (!canCancel) return;

        const key = getKey(args);
        pending.get(key)?.abort();
        pending.delete(key);
    };

    const isCancelledError = (e: unknown): boolean => axios.isCancel(e) || (e instanceof DOMException && e.name === "AbortError");

    return {
        execute,
        cancel,
        isCancelledError
    };
}
