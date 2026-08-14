import axios from "axios";
import type { AxiosInstance } from "axios";
import { AxiosAuthInterceptor } from "@samhammer/authentication-vue";

class Axios {
    public api: AxiosInstance;

    public constructor() {
        this.api = this.createApi(import.meta.env.VITE_API_BASE_URL);
    }

    private createApi(baseURL: string): AxiosInstance {
        const client = axios.create();
        client.defaults.baseURL = baseURL;
        client.defaults.transformResponse = transformResponse;
        client.defaults.paramsSerializer = (params) => {
            const search = new URLSearchParams();

            for (const [key, value] of Object.entries(params)) {
                if (value === null || value === undefined) continue;

                if (Array.isArray(value)) {
                    for (const item of value) {
                        search.append(key, item);
                    }
                    continue;
                }

                search.append(key, value);
            }

            return search.toString();
        };

        AxiosAuthInterceptor.addAuthTokenInterceptor(client);
        AxiosAuthInterceptor.addAuthErrorInterceptor(client);

        return client;
    }
}

export default new Axios();
