import type { Task, TimeEntryEvent, TimeEntryMutation } from "@/components/tracking/calendar/types";
import type { ValidationErrors } from "@/util/ValidationProblem";
import { useChangeSet } from "./useChangeSet";
import { useConflictDetection } from "./useConflictDetection";

export function useEventCommit() {
    const calendarStore = useCalendarStore();
    const changeSet = useChangeSet();
    const conflictDetection = useConflictDetection();

    const { task } = storeToRefs(calendarStore);

    // A rejected mutation becomes the form that fixes it.
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

        // Superseded by a newer edit that owns the state now.
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

    // Suggestions may overlap; only accepting one has to be conflict free.
    const needsConflictCheck = (event: TimeEntryEvent): boolean => {
        const staged = changeSet.get(event.uiId);
        if (!staged) return false;
        if (staged.removed) return false;

        return !(staged.kind === "save" && event.kind === "suggestion");
    };

    // Single exit of a finished edit: an overlap opens the conflict panel, else commit.
    const commitOrEscalate = async (event: TimeEntryEvent): Promise<boolean> => {
        if (needsConflictCheck(event) && conflictDetection.tryEnterConflict(event)) return false;
        return await commitStaged();
    };

    const commitGesture = async (event: TimeEntryEvent): Promise<void> => {
        changeSet.unstageIfUnchanged(event.uiId);

        // Inside a conflict the change stays staged until Apply.
        if (task.value.kind === "conflict") return;
        if (!changeSet.has(event.uiId)) return;

        await commitOrEscalate(event);
    };

    return { commitStaged, commitOrEscalate, commitGesture };
}
