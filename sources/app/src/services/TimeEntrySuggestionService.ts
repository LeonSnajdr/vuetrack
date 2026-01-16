import type { TimeEntrySuggestionContract, TimeEntrySuggestionId, TimeEntrySuggestionUpdateContract } from "@/contracts/TimeEntrySuggestion";

class TimeEntrySuggestionService {
    public async load(): Promise<TimeEntrySuggestionContract[]> {
        await new Promise<void>((resolve) => setTimeout(resolve, 3000));

        return [
            {
                id: "mySuggestion" as TimeEntrySuggestionId,
                startTime: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000 + 60 * 60 * 1000),
                endTime: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000),
                taskId: "my suggested id",
                position: 0
            }
        ];
    }

    public async update(id: TimeEntrySuggestionId, updateContract: TimeEntrySuggestionUpdateContract): Promise<TimeEntrySuggestionContract> {
        await new Promise<void>((resolve) => setTimeout(resolve, 3000));

        console.log("update", id, updateContract);

        return { id, ...updateContract };
    }

    public async dismiss(id: TimeEntrySuggestionId): Promise<void> {
        await new Promise<void>((resolve) => setTimeout(resolve, 3000));

        console.log("dismiss", id);
    }
}

export default new TimeEntrySuggestionService();
