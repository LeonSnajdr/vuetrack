import type { ConfigContract } from "@/contracts/ConfigContract";
import axios from "@/plugins/axios";

class ConfigService {
    public get = async (): Promise<ConfigContract> => {
        const result = await axios.api.get<ConfigContract>("Config");
        return result.data;
    };
}

export default new ConfigService();
