import type { ActivityContract } from "@/contracts/ActivityContract";
import type { ProjectContract, ProjectId } from "@/contracts/ProjectContract";
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

    public findProjectByTaskId = async (taskId: string): Promise<ProjectContract | undefined> => {
        const result = await axios.api.get<ProjectContract | null>("project/findByTaskId", {
            params: { taskId: taskId }
        });

        return result.data ?? undefined;
    };
}

export default new ProjectService();
