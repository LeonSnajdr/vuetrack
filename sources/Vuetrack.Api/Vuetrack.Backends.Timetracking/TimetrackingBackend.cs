using ErrorOr;
using Microsoft.Extensions.Logging;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Backends.Abstractions;
using Vuetrack.Backends.Abstractions.Contracts;
using Vuetrack.Backends.Timetracking.Api;
using Vuetrack.Backends.Timetracking.Connection;
using Vuetrack.Backends.Timetracking.Internal;

namespace Vuetrack.Backends.Timetracking;

[InjectAs(typeof(IBackend))]
public class TimetrackingBackend(ITimetrackingApiClient client, ITimetrackingConnectionAccessor accessor, ILogger<TimetrackingBackend> logger) : IBackend
{
    public const BackendKey Key = BackendKey.Timetracking;

    private ITimetrackingApiClient Client { get; } = client;

    private ITimetrackingConnectionAccessor Accessor { get; } = accessor;

    private ILogger<TimetrackingBackend> Logger { get; } = logger;

    public BackendDescriptor Descriptor { get; } = new()
    {
        Key = Key,
        DisplayName = "Timetracking",
        Capabilities = BackendCapabilities.TimeEntries | BackendCapabilities.Projects | BackendCapabilities.OAuth,
    };

    public async Task<ErrorOr<Success>> ValidateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Client.GetProfileAsync(cancellationToken);
            return Result.Success;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Timetracking validation failed");
            return Error.Unexpected();
        }
    }

    public async Task<ErrorOr<IReadOnlyList<TimeEntryContract>>> GetTimeEntriesAsync(DateRange range, CancellationToken cancellationToken)
    {
        try
        {
            var responses = await Client.GetTimeEntriesAsync(TimetrackingMapper.FormatDate(range.From), TimetrackingMapper.FormatDate(range.To), cancellationToken);
            var contracts = responses.Select(dto => dto.ToContract()).ToList();
            return contracts;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get time entries");
            return Error.Unexpected();
        }
    }

    public async Task<ErrorOr<TimeEntryContract>> CreateTimeEntryAsync(TimeEntryCreateContract contract, CancellationToken cancellationToken)
    {
        try
        {
            var form = TimetrackingMapper.ToCreateForm(contract, Accessor.Current?.ExternalUserId);
            var response = await Client.UpsertTimeEntryAsync(form, cancellationToken);
            return response.ToContract();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create time entry");
            return Error.Unexpected();
        }
    }

    public async Task<ErrorOr<TimeEntryContract>> UpdateTimeEntryAsync(string id, TimeEntryUpdateContract contract, CancellationToken cancellationToken)
    {
        try
        {
            var form = TimetrackingMapper.ToUpdateForm(id, contract, Accessor.Current?.ExternalUserId);
            var response = await Client.UpsertTimeEntryAsync(form, cancellationToken);
            return response.ToContract();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update time entry {Id}", id);
            return Error.Unexpected();
        }
    }

    public async Task<ErrorOr<Deleted>> DeleteTimeEntryAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            await Client.DeleteTimeEntriesAsync(id, cancellationToken);
            return Result.Deleted;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete time entry {Id}", id);
            return Error.Unexpected();
        }
    }

    public async Task<ErrorOr<IReadOnlyList<ProjectContract>>> GetProjectsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var responses = await Client.GetProjectsAsync(cancellationToken);
            var projects = responses.Select(dto => dto.ToProjectContract()).ToList();
            return projects;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get projects");
            return Error.Unexpected();
        }
    }

    public async Task<ErrorOr<IReadOnlyList<ActivityContract>>> GetActivitiesAsync(string projectId, CancellationToken cancellationToken)
    {
        try
        {
            var responses = await Client.GetActivitiesAsync(projectId, cancellationToken);
            var activities = responses.Select(dto => dto.ToActivityContract()).ToList();
            return activities;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get activities for project {ProjectId}", projectId);
            return Error.Unexpected();
        }
    }

    public async Task<ErrorOr<ProjectContract?>> FindProjectByTaskIdAsync(string taskId, CancellationToken cancellationToken)
    {
        try
        {
            var projectId = await Client.FindProjectIdByTaskIdAsync(taskId, cancellationToken);
            if (projectId is null)
            {
                return Error.NotFound();
            }

            // TODO Maybe just return the id

            // The legacy endpoint returns only the id; resolve the display name from the project list.
            var projects = await Client.GetProjectsAsync(cancellationToken);
            var match = projects.FirstOrDefault(p => p.Id.ToString(System.Globalization.CultureInfo.InvariantCulture) == projectId);
            return new ProjectContract(projectId, match?.Name ?? string.Empty);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to find project by task id {TaskId}", taskId);
            return Error.Unexpected();
        }
    }
}
