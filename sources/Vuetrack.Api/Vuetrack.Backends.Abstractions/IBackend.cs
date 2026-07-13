using ErrorOr;
using Vuetrack.Backends.Abstractions.Contracts;

namespace Vuetrack.Backends.Abstractions;

public interface IBackend
{
    BackendDescriptor Descriptor { get; }

    Task<ErrorOr<Success>> ValidateAsync(CancellationToken cancellationToken);

    Task<ErrorOr<IReadOnlyList<TimeEntryContract>>> GetTimeEntriesAsync(DateRange range, CancellationToken cancellationToken);

    Task<ErrorOr<TimeEntryContract>> CreateTimeEntryAsync(TimeEntryCreateContract contract, CancellationToken cancellationToken);

    Task<ErrorOr<TimeEntryContract>> UpdateTimeEntryAsync(string id, TimeEntryUpdateContract contract, CancellationToken cancellationToken);

    Task<ErrorOr<Deleted>> DeleteTimeEntryAsync(string id, CancellationToken cancellationToken);

    Task<ErrorOr<IReadOnlyList<ProjectContract>>> GetProjectsAsync(CancellationToken cancellationToken);

    Task<ErrorOr<IReadOnlyList<ActivityContract>>> GetActivitiesAsync(string projectId, CancellationToken cancellationToken);

    Task<ErrorOr<ProjectContract?>> FindProjectByTaskIdAsync(string taskId, CancellationToken cancellationToken);
}
