import type {
    BackendDescriptorContract,
    TimetrackingAuthorizeResponse,
    TimetrackingConnectRequest,
    TimetrackingConnectResponse,
    TimetrackingStatusResponse
} from "@/contracts/BackendContract";
import axios from "@/plugins/axios";

class BackendService {
    public list = async (): Promise<BackendDescriptorContract[]> => {
        const result = await axios.api.get<BackendDescriptorContract[]>("backends");
        return result.data;
    };

    public timetrackingStatus = async (): Promise<TimetrackingStatusResponse> => {
        const result = await axios.api.get<TimetrackingStatusResponse>("backends/timetracking/status");
        return result.data;
    };

    public authorizeTimetracking = async (redirectUri: string): Promise<TimetrackingAuthorizeResponse> => {
        const result = await axios.api.get<TimetrackingAuthorizeResponse>("backends/timetracking/authorize", { params: { redirectUri } });
        return result.data;
    };

    public connectTimetracking = async (request: TimetrackingConnectRequest): Promise<TimetrackingConnectResponse> => {
        const result = await axios.api.post<TimetrackingConnectResponse>("backends/timetracking/callback", request);
        return result.data;
    };

    public disconnectTimetracking = async (): Promise<void> => {
        await axios.api.delete("backends/timetracking");
    };
}

export default new BackendService();
