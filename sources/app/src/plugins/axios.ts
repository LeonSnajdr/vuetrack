import axios from "axios";
import qs from "qs";
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
            return qs.stringify(params, { arrayFormat: "repeat" });
        };

        AxiosAuthInterceptor.addAuthTokenInterceptor(client);
        AxiosAuthInterceptor.addAuthErrorInterceptor(client);

        return client;
    }
}

export default new Axios();
