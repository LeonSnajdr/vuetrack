import { cloneFnJSON, type UseClonedReturn } from "@vueuse/core";
import { isRef, ref as deepRef, shallowRef, toValue, watch, type MaybeRefOrGetter, type Ref, type WatchOptions } from "vue";

export interface UseClonedMappedOptions<TMapped = any> extends WatchOptions {
    clone?: (source: TMapped) => TMapped;
    manual?: boolean;
}

export function useClonedMapped<TSource, TMapped>(
    source: MaybeRefOrGetter<TSource>,
    mapper: (source: TSource) => TMapped,
    options: UseClonedMappedOptions<TMapped> = {}
): UseClonedReturn<TMapped> {
    const cloned = deepRef({} as TMapped) as Ref<TMapped>;
    const isModified = shallowRef<boolean>(false);
    let _lastSync = false;

    const {
        manual,
        clone = cloneFnJSON,
        deep = true,
        immediate = true
    } = options;

    watch(
        cloned,
        () => {
            if (_lastSync) {
                _lastSync = false;
                return;
            }

            isModified.value = true;
        },
        {
            deep: true,
            flush: "sync"
        }
    );

    function sync(): void {
        _lastSync = true;
        isModified.value = false;

        cloned.value = clone(mapper(toValue(source)));
    }

    if (!manual && (isRef(source) || typeof source === "function")) {
        watch(source, sync, {
            ...options,
            deep,
            immediate
        });
    } else {
        sync();
    }

    return { cloned, isModified, sync };
}
