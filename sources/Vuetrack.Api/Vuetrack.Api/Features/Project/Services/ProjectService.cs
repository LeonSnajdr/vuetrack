using ErrorOr;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Backends;
using Vuetrack.Backends.Abstractions;
using Vuetrack.Backends.Abstractions.Contracts;

namespace Vuetrack.Api.Features.Project.Services;

[Inject]
public class ProjectService(IBackendResolver resolver) : IProjectService
{
    private const BackendKey Backend = BackendKey.Timetracking;

    private IBackendResolver Resolver { get; } = resolver;

    public async Task<ErrorOr<IReadOnlyList<ProjectContract>>> ListAsync(string userId, CancellationToken cancellationToken)
    {
        var backend = await Resolver.ResolveConnectedAsync(Backend, userId, cancellationToken);
        if (backend.IsError)
        {
            return backend.Errors;
        }

        return await backend.Value.GetProjectsAsync(cancellationToken);
    }

    public async Task<ErrorOr<IReadOnlyList<ActivityContract>>> ListActivitiesAsync(string userId, string projectId, CancellationToken cancellationToken)
    {
        var backend = await Resolver.ResolveConnectedAsync(Backend, userId, cancellationToken);
        if (backend.IsError)
        {
            return backend.Errors;
        }

        return await backend.Value.GetActivitiesAsync(projectId, cancellationToken);
    }

    public async Task<ErrorOr<ProjectContract?>> FindByTaskIdAsync(string userId, string taskId, CancellationToken cancellationToken)
    {
        var backend = await Resolver.ResolveConnectedAsync(Backend, userId, cancellationToken);
        if (backend.IsError)
        {
            return backend.Errors;
        }

        return await backend.Value.FindProjectByTaskIdAsync(taskId, cancellationToken);
    }
}

public interface IProjectService
{
    Task<ErrorOr<IReadOnlyList<ProjectContract>>> ListAsync(string userId, CancellationToken cancellationToken);

    Task<ErrorOr<IReadOnlyList<ActivityContract>>> ListActivitiesAsync(string userId, string projectId, CancellationToken cancellationToken);

    Task<ErrorOr<ProjectContract?>> FindByTaskIdAsync(string userId, string taskId, CancellationToken cancellationToken);
}
