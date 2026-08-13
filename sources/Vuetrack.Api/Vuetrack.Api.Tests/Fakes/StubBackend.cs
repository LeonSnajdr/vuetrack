using ErrorOr;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Contracts;
using Vuetrack.Api.Features.Project.Contracts;
using Vuetrack.Api.Features.TimeEntry.Contracts;

namespace Vuetrack.Api.Tests.Fakes;

public class StubBackend : IBackend
{
    public ErrorOr<IReadOnlyList<TimeEntryContract>> ListResult { get; init; }

    public ErrorOr<TimeEntryContract> CreateResult { get; init; }

    public ErrorOr<TimeEntryContract> UpdateResult { get; init; }

    public ErrorOr<Deleted> DeleteResult { get; init; }

    public IntegrationKey Key => IntegrationKey.Timetracking;

    public Task<ErrorOr<Success>> ValidateAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult<ErrorOr<Success>>(Result.Success);

    public Task<ErrorOr<IReadOnlyList<TimeEntryContract>>> GetTimeEntriesAsync(string userId, DateRange range, CancellationToken cancellationToken) =>
        Task.FromResult(ListResult);

    public Task<ErrorOr<TimeEntryContract>> CreateTimeEntryAsync(string userId, TimeEntryCreateContract contract, CancellationToken cancellationToken) =>
        Task.FromResult(CreateResult);

    public Task<ErrorOr<TimeEntryContract>> UpdateTimeEntryAsync(string userId, string id, TimeEntryUpdateContract contract, CancellationToken cancellationToken) =>
        Task.FromResult(UpdateResult);

    public Task<ErrorOr<Deleted>> DeleteTimeEntryAsync(string userId, string id, CancellationToken cancellationToken) =>
        Task.FromResult(DeleteResult);

    public Task<ErrorOr<IReadOnlyList<ProjectContract>>> GetProjectsAsync(string userId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<ErrorOr<IReadOnlyList<ActivityContract>>> GetActivitiesAsync(string userId, string projectId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<ErrorOr<ProjectLookupContract>> FindProjectIdByTaskIdAsync(string userId, string taskId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
