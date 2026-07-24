import type { BackendAuthorizeResponse, BackendConnectRequest, BackendDescriptorContract } from "@/contracts/BackendContract";
import axios from "@/plugins/axios";

class BackendService {
    public list = async (): Promise<BackendDescriptorContract[]> => {
        const result = await axios.api.get<BackendDescriptorContract[]>("backends");
        return result.data;
    };

    public authorize = async (key: string, redirectUri: string): Promise<BackendAuthorizeResponse> => {
        const result = await axios.api.get<BackendAuthorizeResponse>(`backends/${key}/authorize`, { params: { redirectUri } });
        return result.data;
    };

    public connect = async (key: string, request: BackendConnectRequest): Promise<void> => {
        await axios.api.post(`backends/${key}/callback`, request);
    };

    public disconnect = async (key: string): Promise<void> => {
        await axios.api.delete(`backends/${key}`);
    };
}

export default new BackendService();
