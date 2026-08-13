using ErrorOr;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Contracts;
using Vuetrack.Api.Features.Project.Contracts;
using Vuetrack.Api.Features.TimeEntry.Contracts;

namespace Vuetrack.Api.Features.Integrations.Abstractions;

public interface IBackend : IIntegration
{
    Task<ErrorOr<IReadOnlyList<TimeEntryContract>>> GetTimeEntriesAsync(string userId, DateRange range, CancellationToken cancellationToken);

    Task<ErrorOr<TimeEntryContract>> CreateTimeEntryAsync(string userId, TimeEntryCreateContract contract, CancellationToken cancellationToken);

    Task<ErrorOr<TimeEntryContract>> UpdateTimeEntryAsync(string userId, string id, TimeEntryUpdateContract contract, CancellationToken cancellationToken);

    Task<ErrorOr<Deleted>> DeleteTimeEntryAsync(string userId, string id, CancellationToken cancellationToken);

    Task<ErrorOr<IReadOnlyList<ProjectContract>>> GetProjectsAsync(string userId, CancellationToken cancellationToken);

    Task<ErrorOr<IReadOnlyList<ActivityContract>>> GetActivitiesAsync(string userId, string projectId, CancellationToken cancellationToken);

    Task<ErrorOr<ProjectLookupContract>> FindProjectIdByTaskIdAsync(string userId, string taskId, CancellationToken cancellationToken);
}
