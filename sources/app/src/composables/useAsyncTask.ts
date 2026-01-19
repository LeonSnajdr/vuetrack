import type { ActionResult } from "@/util/ActionResult";
import axios from "axios";

export type AsyncWithSignal<TArgs extends unknown[], TResult> = (...args: [...TArgs, AbortSignal?]) => Promise<TResult>;

export interface AsyncTaskKeyContext<TArgs extends unknown[]> {
    args: TArgs;
}

export function useAsyncTask<TArgs extends unknown[], TResult, TKey>(
    fn: AsyncWithSignal<TArgs, TResult>,
    keySelector: (ctx: AsyncTaskKeyContext<TArgs>) => TKey
) {
    const pending = new Map<TKey, AbortController>();

    const execute = async (...args: TArgs): Promise<ActionResult<TResult>> => {
        const key = keySelector({ args });

        // Cancel any pending task with the same key
        pending.get(key)?.abort();

        const controller = new AbortController();
        pending.set(key, controller);

        try {
            const result = await fn(...args, controller.signal);

            return success(result);
        } catch (e) {
            if (isCancelledError(e)) return cancelled();
            console.error(e);
            return error();
        } finally {
            if (pending.get(key) === controller) {
                pending.delete(key);
            }
        }
    };

    const cancel = (...args: TArgs): void => {
        const key = keySelector({ args });
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
