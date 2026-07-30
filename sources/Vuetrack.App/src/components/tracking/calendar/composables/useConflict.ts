import type { ConflictTask, TimeEntryEvent } from "@/components/tracking/calendar/types";
import type { ConflictResolver, Proposal } from "@/components/tracking/calendar/conflictResolvers";
import { useChangeSet } from "./useChangeSet";
import { useConflictDetection } from "./useConflictDetection";
import { useEventCommit } from "./useEventCommit";
import { useEventSelection } from "./useEventSelection";
import { useEventWrapper } from "./useEventWrapper";

export function useConflict() {
    const calendarStore = useCalendarStore();
    const changeSet = useChangeSet();
    const commit = useEventCommit();
    const detection = useConflictDetection();
    const selection = useEventSelection();
    const { cloneAsDraft } = useEventWrapper();

    const { task } = storeToRefs(calendarStore);

    const conflictTask = computed<ConflictTask | null>(() => {
        if (task.value.kind !== "conflict") return null;
        return task.value;
    });

    const selectedEvent = computed<TimeEntryEvent | null>(() => {
        const current = conflictTask.value;
        if (!current) return null;

        return selection.selectedEvent.value ?? current.event;
    });

    const applyProposal = (proposal: Proposal): void => {
        if (proposal.kind === "remove") {
            changeSet.stageRemove(proposal.event);
            return;
        }

        if (proposal.kind === "move") {
            changeSet.stagePosition(proposal.event, proposal.position);
            return;
        }

        const tailEvent = cloneAsDraft(proposal.event, proposal.tail.start, proposal.tail.end);
        changeSet.stagePosition(proposal.event, proposal.head);
        changeSet.stageCreate(tailEvent);
    };

    // Asked first, staged after: a resolution that finds no answer changes nothing at all.
    const previewStrategy = (resolve: ConflictResolver): boolean => {
        if (!conflictTask.value) return false;

        const event = selectedEvent.value;
        if (!event) return false;

        const subject = { event, position: { start: event.start, end: event.end } };
        const proposals = resolve(subject, detection.occupied.value);
        if (!proposals) return false;

        proposals.forEach(applyProposal);
        return true;
    };

    const apply = async () => {
        if (!conflictTask.value) return;

        selection.clearSelection();
        await commit.commitStaged();
    };

    const cancel = () => {
        if (!conflictTask.value) return;

        task.value = { kind: "none" };
        selection.clearSelection();

        changeSet.revertAll();
    };

    return { selectedEvent, previewStrategy, apply, cancel };
}
