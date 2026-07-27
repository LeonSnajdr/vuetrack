import type { PositionableEvent, Task, TimeEntryEvent, TimeEntryMutation } from "@/components/tracking/calendar/types";
import type { ValidationErrors } from "@/util/ValidationProblem";
import { useChangeSet } from "./useChangeSet";
import { useConflictDetection } from "./useConflictDetection";

export function useEventCommit() {
    const calendarStore = useCalendarStore();
    const changeSet = useChangeSet();
    const conflictDetection = useConflictDetection();

    const { task } = storeToRefs(calendarStore);

    // Turns a rejected mutation back into the form the user needs to fix it.
    const buildRecoveryTask = (mutation: TimeEntryMutation, errors: ValidationErrors): Task | null => {
        if (mutation.kind === "update") {
            return { kind: "edit", event: mutation.event, payload: mutation.update, errors };
        }
        if (mutation.kind === "create") {
            return { kind: "create", event: mutation.event, payload: mutation.create, errors };
        }
        return null;
    };

    const commitStaged = async (): Promise<boolean> => {
        const result = await changeSet.commit();
        if (result.status === "success") {
            task.value = { kind: "none" };
            return true;
        }

        // Superseded by a newer edit: whatever state that edit put us in wins.
        if (result.status === "cancelled") return false;

        if (result.validation) {
            const recoveryTask = buildRecoveryTask(result.failedMutation, result.validation);
            if (recoveryTask) {
                task.value = recoveryTask;
                return false;
            }
        }

        task.value = { kind: "none" };
        return false;
    };

    // Suggestions are proposals, not bookings: they may sit on top of stored
    // entries and only have to be conflict free once they are accepted, which
    // stages them as an addition rather than an update.
    const needsConflictCheck = (event: TimeEntryEvent): boolean => {
        const staged = changeSet.get(event.uiId);
        if (!staged) return false;
        if (staged.kind === "remove") return false;

        return !(staged.kind === "update" && event.kind === "suggestion");
    };

    // Single exit point for a finished edit: overlapping events open the
    // conflict panel with the change still staged, everything else commits.
    const commitOrEscalate = async (event: TimeEntryEvent): Promise<boolean> => {
        if (needsConflictCheck(event) && conflictDetection.tryEnterConflict(event)) return false;
        return await commitStaged();
    };

    // Shared tail of a finished move or resize.
    const commitGesture = async (event: PositionableEvent): Promise<void> => {
        changeSet.unstageIfUnchanged(event.uiId);

        // Inside a conflict the change stays staged until the user applies it.
        if (task.value.kind === "conflict") return;
        if (!changeSet.has(event.uiId)) return;

        await commitOrEscalate(event);
    };

    return { commitStaged, commitOrEscalate, commitGesture };
}
