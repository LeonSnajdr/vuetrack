class TimeEntrySuggestionService {
    public async load() {
        await new Promise<void>((resolve) => setTimeout(resolve, 100));

        return [
            {
                id: "mySuggestion" as TimeEntrySuggestionId,
                startTime: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000 + 60 * 60 * 1000),
                endTime: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000),
                taskId: "my suggested id"
            }
        ];
    }
}

export default new TimeEntrySuggestionService();
