import type { TimeEntryCreateContract, TimeEntryId, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";
import type { Nullable } from "@/util/Nullable";

export type Interaction =
    | { kind: "idle" }
    | { kind: "create"; create: Nullable<TimeEntryCreateContract> }
    | { kind: "edit"; timeEntryId: TimeEntryId; update: TimeEntryUpdateContract }
    | { kind: "delete"; timeEntryId: TimeEntryId };
