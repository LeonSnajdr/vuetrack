import type { ConnectorDescriptorContract, JiraAuthorizeResponse, JiraConnectRequest, JiraConnectResponse, JiraStatusResponse } from "@/contracts/ConnectorContract";
import axios from "@/plugins/axios";

class ConnectorService {
    public list = async (): Promise<ConnectorDescriptorContract[]> => {
        const result = await axios.api.get<ConnectorDescriptorContract[]>("connectors");
        return result.data;
    };

    public jiraStatus = async (): Promise<JiraStatusResponse> => {
        const result = await axios.api.get<JiraStatusResponse>("connectors/jira/status");
        return result.data;
    };

    public authorizeJira = async (redirectUri: string): Promise<JiraAuthorizeResponse> => {
        const result = await axios.api.get<JiraAuthorizeResponse>("connectors/jira/authorize", { params: { redirectUri } });
        return result.data;
    };

    public connectJira = async (request: JiraConnectRequest): Promise<JiraConnectResponse> => {
        const result = await axios.api.post<JiraConnectResponse>("connectors/jira/callback", request);
        return result.data;
    };

    public disconnectJira = async (): Promise<void> => {
        await axios.api.delete("connectors/jira");
    };
}

export default new ConnectorService();
