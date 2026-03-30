import type { TimeEntryCreateContract, TimeEntryId, TimeEntryUpdateContract } from "@/contracts/TimeEntryContract";

export type Interaction =
    | { kind: "idle" }
    | { kind: "create"; create: TimeEntryCreateContract }
    | { kind: "edit"; timeEntryId: TimeEntryId; update: TimeEntryUpdateContract }
    | { kind: "delete"; timeEntryId: TimeEntryId };
