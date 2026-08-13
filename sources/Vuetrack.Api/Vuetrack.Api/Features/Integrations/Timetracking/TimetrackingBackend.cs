using ErrorOr;
using Microsoft.Extensions.Logging;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Contracts;
using Vuetrack.Api.Features.Integrations.Timetracking.Api;
using Vuetrack.Api.Features.Integrations.Timetracking.Connection;
using Vuetrack.Api.Features.Integrations.Timetracking.Internal;
using Vuetrack.Api.Features.Project.Contracts;
using Vuetrack.Api.Features.Suggestions.Mappers;
using Vuetrack.Api.Features.TimeEntry.Contracts;

namespace Vuetrack.Api.Features.Integrations.Timetracking;

[InjectAs(typeof(IBackend))]
public class TimetrackingBackend(ITimetrackingApiClient client, ITimetrackingConnectionContextFactory contextFactory, ILogger<TimetrackingBackend> logger) : IBackend
{
    private ITimetrackingApiClient Client { get; } = client;

    private ITimetrackingConnectionContextFactory ContextFactory { get; } = contextFactory;

    private ILogger<TimetrackingBackend> Logger { get; } = logger;

    public IntegrationKey Key => IntegrationKey.Timetracking;

    public Task<ErrorOr<Success>> ValidateAsync(string userId, CancellationToken cancellationToken)
    {
        return RunAsync(
            userId,
            async context =>
            {
                await Client.GetProfileAsync(context.AccessToken, cancellationToken);
                return Result.Success;
            },
            "Timetracking validation failed",
            cancellationToken);
    }

    public Task<ErrorOr<IReadOnlyList<TimeEntryContract>>> GetTimeEntriesAsync(string userId, DateRange range, CancellationToken cancellationToken)
    {
        return RunAsync(
            userId,
            async context =>
            {
                var responses = await Client.GetTimeEntriesAsync(context, range.From.FormatDate(), range.To.FormatDate(), cancellationToken);
                IReadOnlyList<TimeEntryContract> contracts = responses.Select(dto => dto.ToContract()).ToList();
                return contracts;
            },
            "Failed to get time entries",
            cancellationToken);
    }

    public Task<ErrorOr<TimeEntryContract>> CreateTimeEntryAsync(string userId, TimeEntryCreateContract contract, CancellationToken cancellationToken)
    {
        return RunAsync(
            userId,
            async context =>
            {
                var form = contract.ToCreateForm(context.ExternalUserId, approved: false, billable: false);
                var response = await Client.UpsertTimeEntryAsync(context, form, cancellationToken);
                return response.ToContract();
            },
            "Failed to create time entry",
            cancellationToken);
    }

    public Task<ErrorOr<TimeEntryContract>> UpdateTimeEntryAsync(string userId, string id, TimeEntryUpdateContract contract, CancellationToken cancellationToken)
    {
        return RunAsync(
            userId,
            async context =>
            {
                var form = contract.ToUpdateForm(id, context.ExternalUserId, approved: false, billable: false);
                var response = await Client.UpsertTimeEntryAsync(context, form, cancellationToken);
                return response.ToContract();
            },
            $"Failed to update time entry {id}",
            cancellationToken);
    }

    public Task<ErrorOr<Deleted>> DeleteTimeEntryAsync(string userId, string id, CancellationToken cancellationToken)
    {
        return RunAsync(
            userId,
            async context =>
            {
                await Client.DeleteTimeEntriesAsync(context, id, cancellationToken);
                return Result.Deleted;
            },
            $"Failed to delete time entry {id}",
            cancellationToken);
    }

    public Task<ErrorOr<IReadOnlyList<ProjectContract>>> GetProjectsAsync(string userId, CancellationToken cancellationToken)
    {
        return RunAsync(
            userId,
            async context =>
            {
                var responses = await Client.GetProjectsAsync(context, cancellationToken);
                IReadOnlyList<ProjectContract> projects = responses.Select(dto => dto.ToProjectContract()).ToList();
                return projects;
            },
            "Failed to get projects",
            cancellationToken);
    }

    public Task<ErrorOr<IReadOnlyList<ActivityContract>>> GetActivitiesAsync(string userId, string projectId, CancellationToken cancellationToken)
    {
        return RunAsync(
            userId,
            async context =>
            {
                var responses = await Client.GetActivitiesAsync(context, projectId, cancellationToken);
                IReadOnlyList<ActivityContract> activities = responses.Select(dto => dto.ToActivityContract()).ToList();
                return activities;
            },
            $"Failed to get activities for project {projectId}",
            cancellationToken);
    }

    public Task<ErrorOr<ProjectLookupContract>> FindProjectIdByTaskIdAsync(string userId, string taskId, CancellationToken cancellationToken)
    {
        return RunAsync(
            userId,
            async context =>
            {
                var projectId = await Client.FindProjectIdByTaskIdAsync(context, taskId, cancellationToken);
                return new ProjectLookupContract(projectId);
            },
            $"Failed to find project by task id {taskId}",
            cancellationToken);
    }

    private async Task<ErrorOr<TResult>> RunAsync<TResult>(string userId, Func<TimetrackingConnectionContext, Task<TResult>> action, string failureMessage, CancellationToken cancellationToken)
    {
        var context = await ContextFactory.CreateAsync(userId, cancellationToken);
        if (context is null)
        {
            return IntegrationError.NotConnected;
        }

        try
        {
            var result = await action(context);
            return ErrorOrFactory.From(result);
        }
        catch (TimetrackingValidationException ex)
        {
            var errors = ex.ToValidationErrors();
            Logger.LogInformation("Timetracking rejected the request due to validation errors {ValidationErrors}", errors);
            return errors;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{FailureMessage}", failureMessage);
            return Error.Unexpected();
        }
    }
}
