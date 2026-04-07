import type { TrackingFilter } from "@/models/TrackingFilter";
import { useDateHelper } from "@/composables/useDateHelper";
import { formatISO } from "date-fns";

export function useTrackingFilter() {
    const dateHelper = useDateHelper();
    const route = useRoute();
    const router = useRouter();

    const filter = computed((): TrackingFilter => {
        const now = new Date();
        const from = route.query.from ? new Date(route.query.from as string) : dateHelper.startOfWeek(now);
        const to = route.query.to ? new Date(route.query.to as string) : dateHelper.endOfWorkWeek(now);

        return {
            from,
            to
        };
    });

    const updateFilter = async (next: Partial<TrackingFilter>) => {
        const merged = {
            ...filter.value,
            ...next
        };

        const getDate = (date: Date) => formatISO(date, { representation: "date" });

        router.push({ query: { ...route.query, from: getDate(merged.from), to: getDate(merged.to) } });
    };

    return { filter, updateFilter };
}
