import type { ActionResult } from "@/util/ActionResult";
import axios from "axios";

export type AsyncTaskFn<TArgs extends unknown[], TResult> = ((...args: TArgs) => Promise<TResult>) | ((...args: [...TArgs, AbortSignal]) => Promise<TResult>);

export interface AsyncTaskKeyContext<TArgs extends unknown[]> {
    args: TArgs;
}

export type CancelPolicy<TKey, TArgs extends unknown[]> = "none" | "previous" | ((ctx: AsyncTaskKeyContext<TArgs>) => TKey);

export interface UseAsyncTaskConfig<TArgs extends unknown[], TKey> {
    cancelPolicy?: CancelPolicy<TKey, TArgs>;
}

export function useAsyncTask<TArgs extends unknown[], TResult, TKey = symbol>(fn: AsyncTaskFn<TArgs, TResult>, config: UseAsyncTaskConfig<TArgs, TKey> = {}) {
    const { cancelPolicy = "none" } = config;

    const pending = new Map<TKey | symbol, AbortController>();
    const canCancel = cancelPolicy !== "none";

    const getKey = (args: TArgs): TKey | symbol => {
        if (typeof cancelPolicy === "function") {
            return cancelPolicy({ args });
        }
        return Symbol("global");
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
            const result = await (fn as (...a: [...TArgs, AbortSignal]) => Promise<TResult>)(...args, controller?.signal as AbortSignal);

            return success(result);
        } catch (e) {
            if (isCancelledError(e)) {
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
