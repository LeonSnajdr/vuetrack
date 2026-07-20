import type { ConnectorAuthorizeResponse, ConnectorConnectRequest, ConnectorDescriptorContract } from "@/contracts/ConnectorContract";
import axios from "@/plugins/axios";

class ConnectorService {
    public list = async (): Promise<ConnectorDescriptorContract[]> => {
        const result = await axios.api.get<ConnectorDescriptorContract[]>("connectors");
        return result.data;
    };

    public authorize = async (key: string, redirectUri: string): Promise<ConnectorAuthorizeResponse> => {
        const result = await axios.api.get<ConnectorAuthorizeResponse>(`connectors/${key}/authorize`, { params: { redirectUri } });
        return result.data;
    };

    public connect = async (key: string, request: ConnectorConnectRequest): Promise<void> => {
        await axios.api.post(`connectors/${key}/callback`, request);
    };

    public disconnect = async (key: string): Promise<void> => {
        await axios.api.delete(`connectors/${key}`);
    };
}

export default new ConnectorService();
