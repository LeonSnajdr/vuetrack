import type { ActivityContract } from "@/contracts/ActivityContract";
import type { ProjectContract, ProjectId, ProjectLookupContract } from "@/contracts/ProjectContract";
import axios from "@/plugins/axios";

class ProjectService {
    public load = async (): Promise<ProjectContract[]> => {
        const result = await axios.api.get<ProjectContract[]>("project");
        return result.data;
    };

    public loadActivities = async (projectId: ProjectId): Promise<ActivityContract[]> => {
        const result = await axios.api.get<ActivityContract[]>(`project/${projectId}/activity`);
        return result.data;
    };

    public findProjectIdByTaskId = async (taskId: string, signal?: AbortSignal): Promise<ProjectId | null> => {
        const result = await axios.api.get<ProjectLookupContract>("project/findByTaskId", {
            params: { taskId: taskId },
            signal: signal
        });

        return result.data.projectId;
    };
}

export default new ProjectService();
