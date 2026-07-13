using ErrorOr;
using Vuetrack.Api.Features.TimeEntry.Services;
using Vuetrack.Backends.Abstractions.Contracts;

namespace Vuetrack.Api.Tests.Fakes;

public class StubTimeEntryService : ITimeEntryService
{
    public ErrorOr<IReadOnlyList<TimeEntryContract>> ListResult { get; init; }

    public ErrorOr<TimeEntryContract> CreateResult { get; init; }

    public ErrorOr<TimeEntryContract> UpdateResult { get; init; }

    public ErrorOr<Deleted> DeleteResult { get; init; }

    public Task<ErrorOr<IReadOnlyList<TimeEntryContract>>> ListAsync(string userId, DateTime from, DateTime to, CancellationToken cancellationToken) =>
        Task.FromResult(ListResult);

    public Task<ErrorOr<TimeEntryContract>> CreateAsync(string userId, TimeEntryCreateContract contract, CancellationToken cancellationToken) =>
        Task.FromResult(CreateResult);

    public Task<ErrorOr<TimeEntryContract>> UpdateAsync(string userId, string id, TimeEntryUpdateContract contract, CancellationToken cancellationToken) =>
        Task.FromResult(UpdateResult);

    public Task<ErrorOr<Deleted>> DeleteAsync(string userId, string id, CancellationToken cancellationToken) =>
        Task.FromResult(DeleteResult);
}
