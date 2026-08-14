import type { IntegrationAuthorizeResponse, IntegrationConnectRequest, IntegrationContract, IntegrationKey } from "@/contracts/IntegrationContract";
import axios from "@/plugins/axios";

class IntegrationService {
    public list = async (): Promise<IntegrationContract[]> => {
        const result = await axios.api.get<IntegrationContract[]>("integrations");
        return result.data;
    };

    public authorize = async (key: IntegrationKey, redirectUri: string): Promise<IntegrationAuthorizeResponse> => {
        const result = await axios.api.get<IntegrationAuthorizeResponse>(`integrations/${key}/authorize`, { params: { redirectUri } });
        return result.data;
    };

    public connect = async (key: IntegrationKey, request: IntegrationConnectRequest): Promise<void> => {
        await axios.api.post(`integrations/${key}/callback`, request);
    };

    public disconnect = async (key: IntegrationKey): Promise<void> => {
        await axios.api.delete(`integrations/${key}`);
    };
}

export default new IntegrationService();
