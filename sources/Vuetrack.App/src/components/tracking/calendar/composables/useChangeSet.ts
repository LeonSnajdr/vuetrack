import type {
    CreatableEvent,
    EventPosition,
    PositionableEvent,
    StagedChange,
    TimeEntryCreatePayload,
    TimeEntryEvent,
    TimeEntryMutation,
    TimeEntryUpdatePayload
} from "@/components/tracking/calendar/types";
import { useCalendarHelper } from "./useCalendarHelper";
import { useEventMutation } from "./useEventMutation";
import type { ExecuteAllResult } from "./useEventMutation";

export type ChangeSetSnapshot = {
    changes: Map<string, StagedChange>;
    positions: Map<string, EventPosition>;
};

export function useChangeSet() {
    const calendarStore = useCalendarStore();
    const mutation = useEventMutation();
    const { applyEventPosition, buildCreateMutation, buildDeleteMutation, buildUpdateMutation, isRangeOverlapping } = useCalendarHelper();

    const { stagedChanges, activeCommits, isCommittingChanges, draftEvents, events } = storeToRefs(calendarStore);

    const changes = computed<StagedChange[]>(() => [...stagedChanges.value.values()]);
    const count = computed(() => stagedChanges.value.size);
    const removalCount = computed(() => changes.value.filter((change) => change.kind === "remove").length);

    const get = (uiId: string): StagedChange | undefined => {
        return stagedChanges.value.get(uiId);
    };

    const has = (uiId: string): boolean => {
        return stagedChanges.value.has(uiId);
    };

    const isRemoved = (uiId: string): boolean => {
        return get(uiId)?.kind === "remove";
    };

    // First stage wins for `from`, so repeated drags still point at the original position.
    const stageUpdate = (event: PositionableEvent, payload?: TimeEntryUpdatePayload): void => {
        const staged = get(event.uiId);

        // A pending creation already carries the live position.
        if (staged?.kind === "add") return;

        if (staged?.kind === "update") {
            if (payload) staged.payload = payload;
            return;
        }

        stagedChanges.value.set(event.uiId, { kind: "update", event, from: { start: event.start, end: event.end }, payload });
    };

    const stageAdd = (event: CreatableEvent, payload: TimeEntryCreatePayload): void => {
        stagedChanges.value.set(event.uiId, { kind: "add", event, payload });
    };

    // Removing something that was never created just drops the pending creation.
    const stageRemove = (event: TimeEntryEvent): void => {
        const staged = get(event.uiId);

        if (staged?.kind === "add") {
            revert(event.uiId);
            return;
        }

        stagedChanges.value.set(event.uiId, { kind: "remove", event });
    };

    const unstage = (uiId: string): void => {
        stagedChanges.value.delete(uiId);
    };

    // A drag that ended where it started must not mark the event unsaved.
    const unstageIfUnchanged = (uiId: string): void => {
        const staged = get(uiId);
        if (staged?.kind !== "update") return;
        if (staged.payload) return;
        if (staged.event.start !== staged.from.start || staged.event.end !== staged.from.end) return;

        unstage(uiId);
    };

    const revert = (uiId: string): void => {
        const staged = get(uiId);
        if (!staged) return;

        unstage(uiId);

        if (staged.kind === "update") {
            applyEventPosition(staged.event, staged.from.start, staged.from.end);
            return;
        }

        if (staged.kind === "add" && staged.event.kind === "draft") {
            mutation.removeDraftEvent(staged.event.uiId);
        }
    };

    const revertAll = (): void => {
        for (const uiId of [...stagedChanges.value.keys()]) {
            revert(uiId);
        }
    };

    // Try something out and put it back without losing what was already staged.
    const snapshot = (): ChangeSetSnapshot => {
        const changes = new Map<string, StagedChange>();

        for (const [uiId, change] of stagedChanges.value) {
            const copy = change.kind === "update" ? { ...change, from: { ...change.from } } : { ...change };
            changes.set(uiId, copy);
        }

        const positions = new Map<string, EventPosition>();

        for (const event of events.value) {
            positions.set(event.uiId, { start: event.start, end: event.end });
        }

        return { changes, positions };
    };

    const restore = (taken: ChangeSetSnapshot): void => {
        const added = draftEvents.value.filter((event) => !taken.positions.has(event.uiId));

        for (const event of added) {
            mutation.removeDraftEvent(event.uiId);
        }

        for (const event of events.value) {
            const position = taken.positions.get(event.uiId);
            if (!position) continue;

            applyEventPosition(event, position.start, position.end);
        }

        stagedChanges.value = taken.changes;
    };

    const buildMutation = (change: StagedChange): TimeEntryMutation => {
        if (change.kind === "remove") return buildDeleteMutation(change.event);
        if (change.kind === "update") return buildUpdateMutation(change.event, change.payload);
        return buildCreateMutation(change.event, change.payload);
    };

    // Additions hold nothing yet, removals want nothing.
    const getPersistedRange = (change: StagedChange): EventPosition | null => {
        if (change.kind === "add") return null;
        if (change.kind === "update") return change.from;
        return change.event;
    };

    const getTargetRange = (change: StagedChange): EventPosition | null => {
        if (change.kind === "remove") return null;
        return change.event;
    };

    // Saving into a range another entry still holds is rejected by the backend.
    const isBlocked = (change: StagedChange, pending: StagedChange[]): boolean => {
        const target = getTargetRange(change);
        if (!target) return false;

        return pending.some((other) => {
            if (other === change) return false;

            const persisted = getPersistedRange(other);
            if (!persisted) return false;

            return isRangeOverlapping(target, persisted);
        });
    };

    // Takes the first change nothing blocks, so freeing space happens before using it.
    // Two entries swapping places have no valid order; those keep staging order.
    const getOrderedChanges = (): StagedChange[] => {
        const pending = [...changes.value];
        const ordered: StagedChange[] = [];

        while (pending.length > 0) {
            const freeIndex = pending.findIndex((change) => !isBlocked(change, pending));
            const nextIndex = freeIndex === -1 ? 0 : freeIndex;
            const [next] = pending.splice(nextIndex, 1);

            ordered.push(next);
        }

        return ordered;
    };

    type CommitEntry = {
        change: StagedChange;
        mutation: TimeEntryMutation;
        sentPosition: EventPosition;
    };

    const buildCommitPlan = (): CommitEntry[] => {
        const ordered = getOrderedChanges();

        return ordered.map((change) => ({
            change,
            mutation: buildMutation(change),
            sentPosition: { start: change.event.start, end: change.event.end }
        }));
    };

    // An event that moved again while the request was in flight stays staged.
    const settleCommitted = (entry: CommitEntry): void => {
        const uiId = entry.change.event.uiId;
        const staged = get(uiId);
        if (staged !== entry.change) return;

        if (entry.change.kind === "update") {
            const hasMoved = entry.change.event.start !== entry.sentPosition.start || entry.change.event.end !== entry.sentPosition.end;
            if (hasMoved) {
                entry.change.from = entry.sentPosition;
                return;
            }
        }

        unstage(uiId);
    };

    // Saved changes settle as they go, so a failure leaves only outstanding work staged.
    const commit = async (): Promise<ExecuteAllResult> => {
        if (count.value === 0) return { status: "success" };

        activeCommits.value++;

        const plan = buildCommitPlan();
        const mutations = plan.map((entry) => entry.mutation);

        const onExecuted = (executed: TimeEntryMutation) => {
            const entry = plan.find((candidate) => candidate.mutation === executed);
            if (entry) settleCommitted(entry);
        };

        const result = await mutation.executeAll(mutations, onExecuted);

        activeCommits.value--;

        if (result.status === "success") return result;

        // Superseded by a newer edit that owns the state now.
        if (result.status === "cancelled") return result;

        if (!result.validation) revertAll();

        return result;
    };

    return {
        changes,
        count,
        removalCount,
        isCommitting: isCommittingChanges,
        get,
        has,
        isRemoved,
        stageUpdate,
        stageAdd,
        stageRemove,
        unstage,
        unstageIfUnchanged,
        revert,
        revertAll,
        snapshot,
        restore,
        commit
    };
}
