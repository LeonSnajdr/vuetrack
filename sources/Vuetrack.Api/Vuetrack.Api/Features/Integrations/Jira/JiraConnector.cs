using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using ErrorOr;
using Microsoft.Extensions.Options;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Details;
using Vuetrack.Api.Features.Details.Contracts;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Activity;
using Vuetrack.Api.Features.Integrations.Jira.Api;
using Vuetrack.Api.Features.Integrations.Jira.Connection;
using Vuetrack.Api.Features.Integrations.Jira.Internal;

namespace Vuetrack.Api.Features.Integrations.Jira;

[InjectAs(typeof(IConnector))]
public partial class JiraConnector(IJiraSessionFactory sessions, IOptions<JiraOptions> options) : IConnector
{
    private IJiraSessionFactory Sessions { get; } = sessions;

    private IOptions<JiraOptions> Options { get; } = options;

    public IntegrationKey Key => IntegrationKey.Jira;

    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9]+-\d+$")]
    private static partial Regex IssueKeyRegex();

    public async Task<ErrorOr<Success>> ValidateAsync(string userId, CancellationToken cancellationToken)
    {
        var session = await Sessions.OpenAsync(userId, cancellationToken);
        if (session is null)
        {
            return IntegrationError.NotConnected;
        }

        try
        {
            var accountId = await session.GetMyAccountIdAsync(cancellationToken);

            if (string.IsNullOrEmpty(accountId))
            {
                return Error.Validation(description: "Jira did not return an account for these credentials.");
            }

            return Result.Success;
        }
        catch (JiraApiException ex)
        {
            return MapError(ex);
        }
    }

    public async Task<ErrorOr<IReadOnlyList<ActivitySignal>>> FetchAsync(string userId, DateRange range, CancellationToken cancellationToken)
    {
        var session = await Sessions.OpenAsync(userId, cancellationToken);
        if (session is null)
        {
            return IntegrationError.NotConnected;
        }

        try
        {
            var accountId = await session.GetMyAccountIdAsync(cancellationToken);
            var issueResponses = await session.SearchCandidateIssuesAsync(range, cancellationToken);
            var issues = issueResponses
                .Where(i => !string.IsNullOrEmpty(i.Key))
                .Select(JiraIssueContext.FromResponse)
                .ToList();

            var signals = new ConcurrentDictionary<string, ActivitySignal>(StringComparer.Ordinal);

            var maxConcurrency = Math.Max(1, Options.Value.MaxConcurrency);
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = maxConcurrency, CancellationToken = cancellationToken };

            await Parallel.ForEachAsync(issues, parallelOptions, async (issue, ct) =>
            {
                var worklogTask = AddWorklogSignalsAsync(signals, session, issue, accountId, range, ct);
                var commentTask = AddCommentSignalsAsync(signals, session, issue, accountId, range, ct);
                await Task.WhenAll(worklogTask, commentTask);
            });

            await AddChangeSignalsAsync(signals, session, issues, accountId, range, cancellationToken);

            IReadOnlyList<ActivitySignal> result = signals.Values.ToList();
            var errorOr = result.ToErrorOr();
            return errorOr;
        }
        catch (JiraApiException ex)
        {
            return MapError(ex);
        }
    }

    public async Task<ErrorOr<IReadOnlyList<DetailField>>> GetDetailsAsync(string userId, DetailQuery query, CancellationToken cancellationToken)
    {
        var issueKey = query.TaskId?.Trim();

        if (string.IsNullOrEmpty(issueKey) || !IssueKeyRegex().IsMatch(issueKey))
        {
            IReadOnlyList<DetailField> empty = [];
            return empty.ToErrorOr();
        }

        var session = await Sessions.OpenAsync(userId, cancellationToken);
        if (session is null)
        {
            return IntegrationError.NotConnected;
        }

        try
        {
            var issue = await session.GetIssueAsync(issueKey, cancellationToken);
            var fields = issue.ToDetailFields(issueKey, session.SiteUrl);
            return fields.ToErrorOr();
        }
        catch (JiraApiException ex)
        {
            return MapError(ex);
        }
    }

    private async Task AddWorklogSignalsAsync(ConcurrentDictionary<string, ActivitySignal> signals, IJiraSession session, JiraIssueContext issue, string accountId, DateRange window, CancellationToken cancellationToken)
    {
        var worklogs = await session.GetWorklogsAsync(issue.Key, window.From, cancellationToken);

        foreach (var worklog in worklogs)
        {
            if (string.IsNullOrEmpty(worklog.Id) || !IsAuthor(worklog.Author, accountId))
            {
                continue;
            }

            if (worklog.Started is not { } started)
            {
                continue;
            }

            if (started < window.From || started >= window.To)
            {
                continue;
            }

            var signal = worklog.ToWorklogSignal(issue);
            signals[signal.ExternalId] = signal;
        }
    }

    private async Task AddCommentSignalsAsync(ConcurrentDictionary<string, ActivitySignal> signals, IJiraSession session, JiraIssueContext issue, string accountId, DateRange window, CancellationToken cancellationToken)
    {
        var comments = await session.GetCommentsAsync(issue.Key, cancellationToken);

        foreach (var comment in comments)
        {
            if (string.IsNullOrEmpty(comment.Id) || !IsAuthor(comment.Author, accountId))
            {
                continue;
            }

            if (comment.Created is not { } created)
            {
                continue;
            }

            if (created < window.From || created >= window.To)
            {
                continue;
            }

            var signal = comment.ToCommentSignal(issue);
            signals[signal.ExternalId] = signal;
        }
    }

    private async Task AddChangeSignalsAsync(ConcurrentDictionary<string, ActivitySignal> signals, IJiraSession session, IReadOnlyList<JiraIssueContext> issues, string accountId, DateRange window, CancellationToken cancellationToken)
    {
        var issueById = new Dictionary<string, JiraIssueContext>(StringComparer.Ordinal);
        var issueIds = new List<string>();

        foreach (var issue in issues)
        {
            if (string.IsNullOrEmpty(issue.Id))
            {
                continue;
            }

            issueById[issue.Id] = issue;
            issueIds.Add(issue.Id);
        }

        if (issueIds.Count == 0)
        {
            return;
        }

        var changeLogs = await session.GetChangelogsAsync(issueIds, cancellationToken);

        foreach (var changeLog in changeLogs)
        {
            if (changeLog.IssueId is not { } issueId || !issueById.TryGetValue(issueId, out var issue))
            {
                continue;
            }

            if (changeLog.ChangeHistories is null)
            {
                continue;
            }

            foreach (var history in changeLog.ChangeHistories)
            {
                AddHistorySignals(signals, issue, history, accountId, window);
            }
        }
    }

    private static void AddHistorySignals(ConcurrentDictionary<string, ActivitySignal> signals, JiraIssueContext issue, JiraChangelogResponse history, string accountId, DateRange window)
    {
        if (!IsAuthor(history.Author, accountId) || string.IsNullOrEmpty(history.Id) || history.Items is null)
        {
            return;
        }

        if (history.Created is not { } created)
        {
            return;
        }

        if (created < window.From || created >= window.To)
        {
            return;
        }

        for (var index = 0; index < history.Items.Count; index++)
        {
            var item = history.Items[index];
            var kind = item.Classify();
            var signal = history.ToChangeSignal(issue, item, kind, index);
            signals[signal.ExternalId] = signal;
        }
    }

    private static bool IsAuthor(JiraUserResponse? author, string accountId)
    {
        return author?.AccountId is { } id && string.Equals(id, accountId, StringComparison.Ordinal);
    }

    private static Error MapError(JiraApiException ex)
    {
        return ex.Kind switch
        {
            JiraApiErrorKind.Auth => Error.Unauthorized(),
            _ => Error.Failure(),
        };
    }
}
