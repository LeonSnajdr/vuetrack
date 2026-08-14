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
    type TimeEntryMutation,
    type UiId
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

    const get = (uiId: UiId): StagedChange | undefined => {
        return stagedChanges.value.get(uiId);
    };

    const has = (uiId: UiId): boolean => {
        return stagedChanges.value.has(uiId);
    };

    const isRemoved = (uiId: UiId): boolean => {
        return get(uiId)?.removed === true;
    };

    const isSaving = (uiId: UiId): boolean => {
        if (!isCommittingChanges.value) return false;
        return has(uiId);
    };

    const stageSave = (event: PositionableEvent): StagedSaveChange => {
        const staged = get(event.uiId);
        if (staged?.kind === "save") return staged;

        const payload = buildUpdatePayload(event);
        stagedChanges.value.set(event.uiId, { kind: "save", event, payload, removed: false });

        return get(event.uiId) as StagedSaveChange;
    };

    const stageCreate = (event: CreatableEvent): StagedCreateChange => {
        const staged = get(event.uiId);
        if (staged?.kind === "create") return staged;

        const proposed = staged ? getPayloadPosition(staged.payload) : null;
        const payload = event.kind === "draft" ? event.createEntry : buildCreatePayload(event);
        stagedChanges.value.set(event.uiId, { kind: "create", event, payload, removed: false });

        const change = get(event.uiId) as StagedCreateChange;

        if (proposed) stagePosition(event, proposed);

        return change;
    };

    const stageChange = (event: TimeEntryEvent): StagedChange => {
        const staged = get(event.uiId);
        if (staged) return staged;

        if (event.kind === "draft") return stageCreate(event);
        return stageSave(event);
    };

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

    const stageRemove = (event: TimeEntryEvent): void => {
        const change = stageChange(event);
        change.removed = true;
    };

    const unstage = (uiId: UiId): void => {
        stagedChanges.value.delete(uiId);
    };

    const unstageIfUnchanged = (uiId: UiId): void => {
        const change = get(uiId);
        if (change?.kind !== "save") return;
        if (change.removed) return;

        const persisted = buildUpdatePayload(change.event);
        if (!isEqual(change.payload, persisted)) return;

        unstage(uiId);
    };

    const restoreRemoved = (uiId: UiId): void => {
        const change = get(uiId);
        if (!change?.removed) return;

        change.removed = false;
        unstageIfUnchanged(uiId);
    };

    const revertAll = (): void => {
        stagedChanges.value.clear();
    };

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

    const getPersistedRange = (change: StagedChange): EventPosition | null => {
        if (change.kind === "create") return null;
        return getPersistedPosition(change.event);
    };

    const getTargetRange = (change: StagedChange): EventPosition | null => {
        if (change.removed) return null;
        return getPayloadPosition(change.payload);
    };

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
        isSaving,
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
