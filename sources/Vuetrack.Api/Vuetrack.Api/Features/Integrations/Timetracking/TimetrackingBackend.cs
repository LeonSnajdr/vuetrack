using ErrorOr;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Timetracking.Api;
using Vuetrack.Api.Features.Integrations.Timetracking.Connection;
using Vuetrack.Api.Features.Integrations.Timetracking.Internal;
using Vuetrack.Api.Features.Project.Contracts;
using Vuetrack.Api.Features.TimeEntry.Contracts;

namespace Vuetrack.Api.Features.Integrations.Timetracking;

[InjectAs(typeof(IBackend))]
public class TimetrackingBackend(ITimetrackingSessionFactory sessions, ILogger<TimetrackingBackend> logger) : IBackend
{
    private ITimetrackingSessionFactory Sessions { get; } = sessions;

    private ILogger<TimetrackingBackend> Logger { get; } = logger;

    public IntegrationKey Key => IntegrationKey.Timetracking;

    public async Task<ErrorOr<Success>> ValidateAsync(string userId, CancellationToken cancellationToken)
    {
        var session = await Sessions.OpenAsync(userId, cancellationToken);
        if (session is null)
        {
            return IntegrationError.NotConnected;
        }

        try
        {
            await session.GetProfileAsync(cancellationToken);
            return Result.Success;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Timetracking validation failed");
            return Error.Unexpected();
        }
    }

    public async Task<ErrorOr<IReadOnlyList<TimeEntryContract>>> GetTimeEntriesAsync(string userId, DateRange range, CancellationToken cancellationToken)
    {
        var session = await Sessions.OpenAsync(userId, cancellationToken);
        if (session is null)
        {
            return IntegrationError.NotConnected;
        }

        try
        {
            var responses = await session.GetTimeEntriesAsync(range.From.FormatDate(), range.To.FormatDate(), cancellationToken);
            return responses.Select(dto => dto.ToContract()).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get time entries");
            return Error.Unexpected();
        }
    }

    public async Task<ErrorOr<TimeEntryContract>> CreateTimeEntryAsync(string userId, TimeEntryCreateContract contract, CancellationToken cancellationToken)
    {
        var session = await Sessions.OpenAsync(userId, cancellationToken);
        if (session is null)
        {
            return IntegrationError.NotConnected;
        }

        try
        {
            var form = contract.ToCreateForm(session.ExternalUserId, approved: false, billable: false);
            var response = await session.UpsertTimeEntryAsync(form, cancellationToken);
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

    public async Task<ErrorOr<TimeEntryContract>> UpdateTimeEntryAsync(string userId, string id, TimeEntryUpdateContract contract, CancellationToken cancellationToken)
    {
        var session = await Sessions.OpenAsync(userId, cancellationToken);
        if (session is null)
        {
            return IntegrationError.NotConnected;
        }

        try
        {
            var form = contract.ToUpdateForm(id, session.ExternalUserId, approved: false, billable: false);
            var response = await session.UpsertTimeEntryAsync(form, cancellationToken);
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

    public async Task<ErrorOr<Deleted>> DeleteTimeEntryAsync(string userId, string id, CancellationToken cancellationToken)
    {
        var session = await Sessions.OpenAsync(userId, cancellationToken);
        if (session is null)
        {
            return IntegrationError.NotConnected;
        }

        try
        {
            await session.DeleteTimeEntriesAsync(id, cancellationToken);
            return Result.Deleted;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete time entry {Id}", id);
            return Error.Unexpected();
        }
    }

    public async Task<ErrorOr<IReadOnlyList<ProjectContract>>> GetProjectsAsync(string userId, CancellationToken cancellationToken)
    {
        var session = await Sessions.OpenAsync(userId, cancellationToken);
        if (session is null)
        {
            return IntegrationError.NotConnected;
        }

        try
        {
            var responses = await session.GetProjectsAsync(cancellationToken);
            return responses.Select(dto => dto.ToProjectContract()).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get projects");
            return Error.Unexpected();
        }
    }

    public async Task<ErrorOr<IReadOnlyList<ActivityContract>>> GetActivitiesAsync(string userId, string projectId, CancellationToken cancellationToken)
    {
        var session = await Sessions.OpenAsync(userId, cancellationToken);
        if (session is null)
        {
            return IntegrationError.NotConnected;
        }

        try
        {
            var responses = await session.GetActivitiesAsync(projectId, cancellationToken);
            return responses.Select(dto => dto.ToActivityContract()).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get activities for project {ProjectId}", projectId);
            return Error.Unexpected();
        }
    }

    public async Task<ErrorOr<ProjectLookupContract>> FindProjectIdByTaskIdAsync(string userId, string taskId, CancellationToken cancellationToken)
    {
        var session = await Sessions.OpenAsync(userId, cancellationToken);
        if (session is null)
        {
            return IntegrationError.NotConnected;
        }

        try
        {
            var projectId = await session.FindProjectIdByTaskIdAsync(taskId, cancellationToken);
            return new ProjectLookupContract(projectId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to find project by task id {TaskId}", taskId);
            return Error.Unexpected();
        }
    }
}
