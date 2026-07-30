import {
    getPayloadPosition,
    type CreatableEvent,
    type EventPosition,
    type PositionableEvent,
    type StagedChange,
    type StagedCreateChange,
    type StagedPayload,
    type StagedSaveChange,
    type TimeEntryEvent,
    type TimeEntryMutation
} from "@/components/tracking/calendar/types";
import { useCalendarHelper } from "./useCalendarHelper";
import { useEventMutation } from "./useEventMutation";
import type { ExecuteAllResult } from "./useEventMutation";
import { isEqual } from "lodash";

export function useChangeSet() {
    const calendarStore = useCalendarStore();
    const mutation = useEventMutation();
    const { buildCreateMutation, buildCreatePayload, buildDeleteMutation, buildUpdateMutation, buildUpdatePayload, getPersistedPosition, isRangeOverlapping } =
        useCalendarHelper();

    const { stagedChanges, activeCommits, isCommittingChanges } = storeToRefs(calendarStore);

    const changes = computed<StagedChange[]>(() => [...stagedChanges.value.values()]);
    const count = computed(() => stagedChanges.value.size);
    const removalCount = computed(() => changes.value.filter((change) => change.removed).length);

    const get = (uiId: string): StagedChange | undefined => {
        return stagedChanges.value.get(uiId);
    };

    const has = (uiId: string): boolean => {
        return stagedChanges.value.has(uiId);
    };

    const isRemoved = (uiId: string): boolean => {
        return get(uiId)?.removed === true;
    };

    // Always handed back out of the map: only that copy notifies the calendar of a write.
    const stageSave = (event: PositionableEvent): StagedSaveChange => {
        const staged = get(event.uiId);
        if (staged?.kind === "save") return staged;

        const payload = buildUpdatePayload(event);
        stagedChanges.value.set(event.uiId, { kind: "save", event, payload, removed: false });

        return get(event.uiId) as StagedSaveChange;
    };

    // Staging the create is what brings a draft into existence, and a draft's proposal is
    // its own createEntry: the two can never drift apart.
    const stageCreate = (event: CreatableEvent): StagedCreateChange => {
        const staged = get(event.uiId);
        if (staged?.kind === "create") return staged;

        const proposed = staged ? getPayloadPosition(staged.payload) : null;
        const payload = event.kind === "draft" ? event.createEntry : buildCreatePayload(event);
        stagedChanges.value.set(event.uiId, { kind: "create", event, payload, removed: false });

        const change = get(event.uiId) as StagedCreateChange;

        // Creating what was already moved keeps it where the user put it.
        if (proposed) stagePosition(event, proposed);

        return change;
    };

    // Whatever is already staged stays: a removal must not turn a pending create into a delete.
    const stageChange = (event: TimeEntryEvent): StagedChange => {
        const staged = get(event.uiId);
        if (staged) return staged;

        if (event.kind === "draft") return stageCreate(event);
        return stageSave(event);
    };

    // The one draft with no staged create is the one still being drawn; it owns its dates.
    const getProposal = (event: TimeEntryEvent): StagedPayload => {
        const staged = get(event.uiId);
        if (staged) return staged.payload;
        if (event.kind === "draft") return event.createEntry;

        return stageSave(event).payload;
    };

    const stagePosition = (event: TimeEntryEvent, position: EventPosition): void => {
        const payload = getProposal(event);

        payload.dateStarted = new Date(position.start);
        payload.dateEnded = new Date(position.end);
    };

    // Marking a removal leaves the proposal underneath intact, so Restore can uncover it.
    const stageRemove = (event: TimeEntryEvent): void => {
        const change = stageChange(event);
        change.removed = true;
    };

    const unstage = (uiId: string): void => {
        stagedChanges.value.delete(uiId);
    };

    // A drag that ended where it started must not mark the event unsaved. Compared whole:
    // a pending field edit is a change even when the times match what is saved.
    const unstageIfUnchanged = (uiId: string): void => {
        const change = get(uiId);
        if (change?.kind !== "save") return;
        if (change.removed) return;

        const persisted = buildUpdatePayload(change.event);
        if (!isEqual(change.payload, persisted)) return;

        unstage(uiId);
    };

    // Uncovers the proposal the removal was hiding. A removal that covered nothing at all
    // leaves nothing behind, so the entry stops counting as a change.
    const restoreRemoved = (uiId: string): void => {
        const change = get(uiId);
        if (!change?.removed) return;

        change.removed = false;
        unstageIfUnchanged(uiId);
    };

    // Nothing to put back: the contract was never written, so dropping the proposal is enough.
    const revertAll = (): void => {
        stagedChanges.value.clear();
    };

    // A removed draft was never saved: there is nothing to send.
    const isCommittable = (change: StagedChange): boolean => {
        return !change.removed || change.kind === "save";
    };

    const buildMutation = (change: StagedChange): TimeEntryMutation | null => {
        if (change.removed) {
            if (change.kind === "create") return null;
            return buildDeleteMutation(change.event);
        }

        if (change.kind === "save") return buildUpdateMutation(change.event, change.payload);
        return buildCreateMutation(change.event, change.payload);
    };

    // A create holds nothing yet, a removal wants nothing.
    const getPersistedRange = (change: StagedChange): EventPosition | null => {
        if (change.kind === "create") return null;
        return getPersistedPosition(change.event);
    };

    const getTargetRange = (change: StagedChange): EventPosition | null => {
        if (change.removed) return null;
        return getPayloadPosition(change.payload);
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
        const pending = changes.value.filter(isCommittable);
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
        sentPosition: EventPosition | null;
    };

    const buildCommitPlan = (): CommitEntry[] => {
        const ordered = getOrderedChanges();

        return ordered.flatMap((change) => {
            const mutation = buildMutation(change);
            if (!mutation) return [];

            const sentPosition = getTargetRange(change);
            return [{ change, mutation, sentPosition }];
        });
    };

    // An event that moved again while the request was in flight stays staged; what was
    // saved is now in the contract, so everything else can simply let go.
    const settleCommitted = (entry: CommitEntry): void => {
        const uiId = entry.change.event.uiId;
        const staged = get(uiId);
        if (staged !== entry.change) return;

        const target = getTargetRange(staged);
        const sent = entry.sentPosition;

        if (target && sent && (target.start !== sent.start || target.end !== sent.end)) return;

        unstage(uiId);
    };

    const dropUncommittable = (): void => {
        for (const [uiId, change] of [...stagedChanges.value]) {
            if (isCommittable(change)) continue;
            unstage(uiId);
        }
    };

    // Saved changes settle as they go, so a failure leaves only outstanding work staged.
    const commit = async (): Promise<ExecuteAllResult> => {
        if (count.value === 0) return { status: "success" };

        dropUncommittable();
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
        stageSave,
        stageCreate,
        stagePosition,
        stageRemove,
        restoreRemoved,
        unstage,
        unstageIfUnchanged,
        revertAll,
        commit
    };
}
