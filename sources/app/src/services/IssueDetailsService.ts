import type { IssueDetailsContract } from "@/contracts/IssueDetailsContract";
import axios from "@/plugins/axios";

// Issue metadata (summary/type/status) for event tooltips. Backend-agnostic: the
// fake api resolves it from Jira; a real backend would do the same server-side.
class IssueDetailsService {
    private readonly cache = new Map<string, Promise<IssueDetailsContract>>();

    public get = (taskId: string): Promise<IssueDetailsContract> => {
        const key = taskId.toLowerCase();
        const cached = this.cache.get(key);
        if (cached) return cached;

        const request = axios.api
            .get<IssueDetailsContract>("issueDetails", { params: { taskId } })
            .then((result) => result.data)
            .catch((error) => {
                // Don't cache failures — allow a later hover to retry.
                this.cache.delete(key);
                throw error;
            });

        this.cache.set(key, request);
        return request;
    };
}

export default new IssueDetailsService();
