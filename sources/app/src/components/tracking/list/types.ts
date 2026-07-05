import type { TimeEntryCreateContract, TimeEntryId, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";
import type { Nullable } from "@/util/Nullable";
import type { ApiValidationError } from "@/util/ApiValidationError";

export type Interaction =
    | { kind: "idle" }
    | { kind: "create"; create: Nullable<TimeEntryCreateContract>; errors?: ApiValidationError }
    | { kind: "edit"; timeEntryId: TimeEntryId; update: TimeEntryUpdateContract; errors?: ApiValidationError }
    | { kind: "delete"; timeEntryId: TimeEntryId };
