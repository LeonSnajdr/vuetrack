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
        Capabilities = [BackendCapabilities.TimeEntries, BackendCapabilities.Projects, BackendCapabilities.OAuth],
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
            var responses = await Client.GetTimeEntriesAsync(range.From.FormatDate(), range.To.FormatDate(), cancellationToken);
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
            var form = contract.ToCreateForm(Accessor.Current?.ExternalUserId, approved: false, billable: false);
            var response = await Client.UpsertTimeEntryAsync(form, cancellationToken);
            return response.ToContract();
        }
        catch (TimetrackingValidationException ex)
        {
            var errors = ex.ToValidationErrors();
            Logger.LogInformation("Timetracking rejected time entry create due to validation errors {ValidationErrors}", errors);
            return errors;
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
            var form = contract.ToUpdateForm(id, Accessor.Current?.ExternalUserId, approved: false, billable: false);
            var response = await Client.UpsertTimeEntryAsync(form, cancellationToken);
            return response.ToContract();
        }
        catch (TimetrackingValidationException ex)
        {
            var errors = ex.ToValidationErrors();
            Logger.LogInformation("Timetracking rejected time entry update of {Id} due to validation errors {ValidationErrors}", id, errors);
            return errors;
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

    public async Task<ErrorOr<ProjectLookupContract>> FindProjectIdByTaskIdAsync(string taskId, CancellationToken cancellationToken)
    {
        try
        {
            var projectId = await Client.FindProjectIdByTaskIdAsync(taskId, cancellationToken);
            return new ProjectLookupContract(projectId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to find project by task id {TaskId}", taskId);
            return Error.Unexpected();
        }
    }
}
