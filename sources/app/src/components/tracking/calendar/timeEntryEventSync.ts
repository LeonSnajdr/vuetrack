import type { Ref } from "vue";
import type { TimeEntryContract } from "@/contracts/TimeEntryContract";
import type { TimeEntrySuggestionContract } from "@/contracts/TimeEntrySuggestion";
import type { ExistingTimeEntryEvent, SuggestionTimeEntryEvent } from "./types";

export default function useMappingToEvents<T extends TimeEntryContract | TimeEntrySuggestionContract>(
    kind: (ExistingTimeEntryEvent | SuggestionTimeEntryEvent)["kind"],
    entriesRef: Ref<T[]>,
    color = "#7da6c9",
    skipSyncFor?: (entry: T) => boolean
) {
    const events = ref<(ExistingTimeEntryEvent | SuggestionTimeEntryEvent)[]>([]);

    watch(
        () => entriesRef.value.slice(),
        (newEntries, oldEntries) => {
            const entries = newEntries || [];
            const previous = oldEntries || [];

            const newEntrySet = new Set(entries);
            const removedEntrySet = new Set(previous.filter((x) => !newEntrySet.has(x)));

            if (removedEntrySet.size > 0) {
                events.value = events.value.filter((e) => e.kind !== kind || !removedEntrySet.has(e.timeEntry as T));
            }

            const eventMap = new Map(events.value.filter((e) => e.kind === kind).map((e) => [e.timeEntry as T, e]));

            const newUIEvents: (ExistingTimeEntryEvent | SuggestionTimeEntryEvent)[] = [];

            for (const x of entries) {
                const existingEvent = eventMap.get(x);

                if (existingEvent) {
                    if (!skipSyncFor?.(x)) {
                        existingEvent.start = x.startTime.getTime();
                        existingEvent.end = x.endTime.getTime();
                    }
                } else {
                    newUIEvents.push({
                        kind: kind,
                        color: color,
                        start: x.startTime.getTime(),
                        end: x.endTime.getTime(),
                        timed: true,
                        uiId: `event-uiId-${uuidv4()}`,
                        timeEntry: x
                    } as ExistingTimeEntryEvent | SuggestionTimeEntryEvent);
                }
            }

            if (newUIEvents.length > 0) {
                events.value.push(...newUIEvents);
            }
        },
        { immediate: true, deep: true }
    );

    return events;
}
