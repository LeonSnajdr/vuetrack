import type { TimeEntryCreateContract, TimeEntryId, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";
import type { Nullable } from "@/util/Nullable";
import type { ValidationErrors } from "@/util/ValidationProblem";

export type Interaction =
    | { kind: "idle" }
    | { kind: "create"; create: Nullable<TimeEntryCreateContract>; errors?: ValidationErrors }
    | { kind: "edit"; timeEntryId: TimeEntryId; update: TimeEntryUpdateContract; errors?: ValidationErrors }
    | { kind: "delete"; timeEntryId: TimeEntryId };
