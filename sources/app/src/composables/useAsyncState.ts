import { type Ref } from "vue";
import { type AsyncTaskFn, type UseAsyncTaskConfig } from "./useAsyncTask";

export interface UseAsyncStateOptions<TArgs extends unknown[], TResult, TKey> extends UseAsyncTaskConfig<TArgs, TKey> {
    shallow?: boolean;
    initialValue?: TResult;
}

export interface UseAsyncStateReturn<TArgs extends unknown[], TResult, TKey> {
    data: Ref<TResult>;
    isLoading: Ref<boolean>;
    error: Ref<boolean>;
    execute: (...args: TArgs) => Promise<void>;
    cancel: (key: TKey | symbol) => void;
}

export function useAsyncState<TArgs extends unknown[], TResult, TKey = symbol>(
    fn: AsyncTaskFn<TArgs, TResult>,
    options: UseAsyncStateOptions<TArgs, TResult, TKey> & { initialValue: TResult }
): UseAsyncStateReturn<TArgs, TResult, TKey>;

export function useAsyncState<TArgs extends unknown[], TResult, TKey = symbol>(
    fn: AsyncTaskFn<TArgs, TResult>,
    options?: UseAsyncStateOptions<TArgs, TResult, TKey>
): UseAsyncStateReturn<TArgs, TResult | null, TKey>;

export function useAsyncState<TArgs extends unknown[], TResult, TKey = symbol>(
    fn: AsyncTaskFn<TArgs, TResult>,
    options: UseAsyncStateOptions<TArgs, TResult, TKey> = {}
): UseAsyncStateReturn<TArgs, TResult | null, TKey> {
    const { shallow = true, initialValue, ...taskConfig } = options;

    const data = (shallow ? shallowRef<TResult | null>(initialValue ?? null) : ref<TResult | null>(initialValue ?? null)) as Ref<TResult | null>;

    const isLoading = ref(false);
    const error = ref(false);

    const { execute: taskExecute, cancel } = useAsyncTask(fn, taskConfig);

    const execute = async (...args: TArgs): Promise<void> => {
        isLoading.value = true;
        error.value = false;

        const result = await taskExecute(...args);

        if (result.status === "success") {
            data.value = result.data as TResult;
        }

        if (result.status === "error") {
            error.value = true;
        }
        isLoading.value = false;
    };

    return {
        data,
        isLoading,
        error,
        execute,
        cancel
    };
}
