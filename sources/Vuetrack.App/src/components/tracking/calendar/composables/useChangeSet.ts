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

// Everything an attempted resolution may touch: the staged changes plus where
// every event sat when it started.
export type ChangeSetSnapshot = {
    changes: Map<string, StagedChange>;
    positions: Map<string, EventPosition>;
};

// Staging buffer for pending changes. Every edit is staged here first, which
// makes it the single source of truth for unsaved state, rollback and commit.
// Committing immediately after staging gives the classic "save on drop"
// behaviour; staging many changes and committing later gives batch resolution.
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

    // The first stage wins for `from`, so repeated drags of the same event keep
    // pointing back at the position it had before the user touched it.
    const stageUpdate = (event: PositionableEvent, payload?: TimeEntryUpdatePayload): void => {
        const staged = get(event.uiId);

        // A pending creation already carries the live position, so moving or
        // resizing it does not need its own staged change.
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

    // Staging a removal for something that was never created just drops the
    // pending creation instead of queueing a delete for the backend.
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

    // Drops a staged move that ended up back where it started, so a cancelled or
    // zero-distance drag neither marks the event unsaved nor sends a request.
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

    // Lets a caller try something out and put everything back if it did not work
    // out, without losing the changes that were already staged.
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

    // The range the backend still holds, and the one the event wants next.
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

    // Saving into a range another entry still occupies is rejected, so a change
    // has to wait for that entry to move out of the way first.
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

    // Repeatedly takes the first change nothing blocks: removals and shrinks
    // end up before the growth and additions that need the freed space, in
    // either direction. Two entries swapping places block each other with no
    // valid order, so those keep their staging order and the backend decides.
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

    // What a commit sent, so its outcome can be matched against whatever the
    // user did in the meantime.
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

    // Saving one change must never throw away an edit made while the request was
    // in flight: the entry only leaves the change set if the event still sits
    // where it was saved. If it moved on, the entry stays staged and its
    // rollback target moves up to what the backend now holds.
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

    // Executes every staged change. Saved ones settle as they go, so a failure
    // leaves exactly the outstanding work staged: the caller can hand the
    // failing event to a form and the rest still flushes on the next commit.
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

        // A cancelled request was superseded by a newer edit that is already
        // being dragged or committed. Touching anything here would yank the
        // event out from under it, so the staged changes are left alone.
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
