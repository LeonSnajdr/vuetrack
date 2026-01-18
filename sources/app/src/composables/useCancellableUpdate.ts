import axios from "axios";

export function useCancellableUpdate<TId extends string>() {
    const pending = new Map<TId, AbortController>();

    const execute = async <T>(id: TId, request: (signal: AbortSignal) => Promise<T>): Promise<T> => {
        pending.get(id)?.abort();
        const controller = new AbortController();
        pending.set(id, controller);
        try {
            return await request(controller.signal);
        } finally {
            if (pending.get(id) === controller) pending.delete(id);
        }
    };

    const isCancelledError = (e: unknown) => axios.isCancel(e) || (e instanceof DOMException && e.name === "AbortError");

    return { execute, isCancelledError };
}
