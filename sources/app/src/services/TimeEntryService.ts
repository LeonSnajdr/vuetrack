class TimeEntryService {
    public async load() {
        await new Promise<void>((resolve) => setTimeout(resolve, 100));

        return [
            {
                id: "myId" as TimeEntryId,
                user: "leon",
                startTime: new Date(),
                endTime: new Date(Date.now() + 60 * 60 * 1000),
                taskId: "myTask"
            }
        ];
    }
}

export default new TimeEntryService();
