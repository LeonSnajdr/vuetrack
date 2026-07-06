import type { IssueDetailsContract } from "@/contracts/IssueDetailsContract";
import axios from "@/plugins/axios";

export const isIssueKey = (taskId: string | null | undefined): boolean => !!taskId && /^[A-Za-z][A-Za-z0-9]+-\d+$/.test(taskId.trim());

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
                this.cache.delete(key);
                throw error;
            });

        this.cache.set(key, request);
        return request;
    };
}

export default new IssueDetailsService();
