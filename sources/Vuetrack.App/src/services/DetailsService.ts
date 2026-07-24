import type { DetailsContract } from "@/contracts/DetailsContract";
import axios from "@/plugins/axios";

class DetailsService {
    private readonly cache = new Map<string, Promise<DetailsContract>>();

    public get = (taskId: string): Promise<DetailsContract> => {
        const key = taskId.toLowerCase();
        const cached = this.cache.get(key);
        if (cached) return cached;

        const request = axios.api
            .get<DetailsContract>("details", { params: { taskId } })
            .then((result) => result.data)
            .catch((error) => {
                this.cache.delete(key);
                throw error;
            });

        this.cache.set(key, request);
        return request;
    };
}

export default new DetailsService();
