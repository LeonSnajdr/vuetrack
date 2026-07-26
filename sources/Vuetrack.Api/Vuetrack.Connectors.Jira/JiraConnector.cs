using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using ErrorOr;
using Microsoft.Extensions.Options;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Jira.Activity;
using Vuetrack.Connectors.Jira.Activity.Api;
using Vuetrack.Connectors.Jira.Connection;

namespace Vuetrack.Connectors.Jira;

[InjectAs(typeof(IConnector))]
public partial class JiraConnector(IJiraApiClient client, IJiraConnectionAccessor accessor, IOptions<JiraOptions> options) : IConnector
{
    public const ConnectorKey Key = ConnectorKey.Jira;

    private IJiraApiClient Client { get; } = client;

    private IJiraConnectionAccessor Accessor { get; } = accessor;

    private IOptions<JiraOptions> Options { get; } = options;

    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9]+-\d+$")]
    private static partial Regex IssueKeyRegex();

    public ConnectorDescriptor Descriptor { get; } = new()
    {
        Key = Key,
        Capabilities = [ConnectorCapabilities.Worklogs, ConnectorCapabilities.IssueActivity, ConnectorCapabilities.OAuth],
    };

    public async Task<ErrorOr<Success>> ValidateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var accountId = await Client.GetMyAccountIdAsync(cancellationToken);

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

    public async Task<ErrorOr<IReadOnlyList<ActivitySignal>>> FetchAsync(ActivityFetchContainer container, CancellationToken cancellationToken)
    {
        try
        {
            var accountId = await Client.GetMyAccountIdAsync(cancellationToken);
            var issueResponses = await Client.SearchCandidateIssuesAsync(container.From, container.To, cancellationToken);
            var contexts = issueResponses
                .Where(i => !string.IsNullOrEmpty(i.Key))
                .Select(JiraIssueContext.FromResponse)
                .ToList();

            // Keyed by ExternalId so overlapping fetch windows collapse deterministically before the engine.
            var signals = new ConcurrentDictionary<string, ActivitySignal>(StringComparer.Ordinal);

            var maxConcurrency = Math.Max(1, Options.Value.MaxConcurrency);
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = maxConcurrency, CancellationToken = cancellationToken };

            await Parallel.ForEachAsync(contexts, parallelOptions, async (context, ct) =>
            {
                var worklogTask = AddWorklogSignalsAsync(signals, context, accountId, container, ct);
                var commentTask = AddCommentSignalsAsync(signals, context, accountId, container, ct);
                await Task.WhenAll(worklogTask, commentTask);
            });

            await AddChangeSignalsAsync(signals, contexts, accountId, container, cancellationToken);

            IReadOnlyList<ActivitySignal> result = signals.Values.ToList();
            var errorOr = result.ToErrorOr();
            return errorOr;
        }
        catch (JiraApiException ex)
        {
            return MapError(ex);
        }
    }

    public async Task<ErrorOr<IReadOnlyList<DetailField>>> GetDetailsAsync(DetailQuery query, CancellationToken cancellationToken)
    {
        var issueKey = query.TaskId?.Trim();

        if (string.IsNullOrEmpty(issueKey) || !IssueKeyRegex().IsMatch(issueKey))
        {
            IReadOnlyList<DetailField> empty = [];
            return empty.ToErrorOr();
        }

        try
        {
            var issue = await Client.GetIssueAsync(issueKey, cancellationToken);
            var siteUrl = Accessor.Current?.SiteUrl ?? string.Empty;
            var fields = issue.ToDetailFields(issueKey, siteUrl);
            return fields.ToErrorOr();
        }
        catch (JiraApiException ex)
        {
            return MapError(ex);
        }
    }

    private async Task AddWorklogSignalsAsync(ConcurrentDictionary<string, ActivitySignal> signals, JiraIssueContext context, string accountId, ActivityFetchContainer window, CancellationToken cancellationToken)
    {
        var worklogs = await Client.GetWorklogsAsync(context.Key, window.From, cancellationToken);

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

            var signal = worklog.ToWorklogSignal(context);
            signals[signal.ExternalId] = signal;
        }
    }

    private async Task AddCommentSignalsAsync(ConcurrentDictionary<string, ActivitySignal> signals, JiraIssueContext context, string accountId, ActivityFetchContainer window, CancellationToken cancellationToken)
    {
        var comments = await Client.GetCommentsAsync(context.Key, cancellationToken);

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

            var signal = comment.ToCommentSignal(context);
            signals[signal.ExternalId] = signal;
        }
    }

    private async Task AddChangeSignalsAsync(ConcurrentDictionary<string, ActivitySignal> signals, IReadOnlyList<JiraIssueContext> contexts, string accountId, ActivityFetchContainer window, CancellationToken cancellationToken)
    {
        var contextById = new Dictionary<string, JiraIssueContext>(StringComparer.Ordinal);
        var issueIds = new List<string>();

        foreach (var context in contexts)
        {
            if (string.IsNullOrEmpty(context.Id))
            {
                continue;
            }

            contextById[context.Id] = context;
            issueIds.Add(context.Id);
        }

        if (issueIds.Count == 0)
        {
            return;
        }

        var changeLogs = await Client.GetChangelogsAsync(issueIds, cancellationToken);

        foreach (var changeLog in changeLogs)
        {
            if (changeLog.IssueId is not { } issueId || !contextById.TryGetValue(issueId, out var context))
            {
                continue;
            }

            if (changeLog.ChangeHistories is null)
            {
                continue;
            }

            foreach (var history in changeLog.ChangeHistories)
            {
                AddHistorySignals(signals, context, history, accountId, window);
            }
        }
    }

    private static void AddHistorySignals(ConcurrentDictionary<string, ActivitySignal> signals, JiraIssueContext context, JiraChangelogResponse history, string accountId, ActivityFetchContainer window)
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
            var signal = history.ToChangeSignal(context, item, index);
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
