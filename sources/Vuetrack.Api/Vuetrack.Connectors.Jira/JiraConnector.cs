using ErrorOr;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Jira.Activity;
using Vuetrack.Connectors.Jira.Activity.Dtos;
using Vuetrack.Connectors.Jira.Connection;

namespace Vuetrack.Connectors.Jira;

[InjectAs(typeof(IConnector))]
public class JiraConnector(IJiraApiClient client, IJiraConnectionAccessor accessor) : IConnector
{
    public const ConnectorKey Key = ConnectorKey.Jira;

    private IJiraApiClient Client { get; } = client;

    private IJiraConnectionAccessor Accessor { get; } = accessor;

    public ConnectorDescriptor Descriptor { get; } = new()
    {
        Key = Key,
        DisplayName = "Jira",
        Capabilities = ConnectorCapabilities.Worklogs | ConnectorCapabilities.IssueActivity | ConnectorCapabilities.OAuth,
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
            var siteUrl = Accessor.Current?.SiteUrl ?? string.Empty;

            var issueDtos = await Client.SearchCandidateIssuesAsync(container.From, container.To, cancellationToken);
            var contexts = issueDtos
                .Where(i => !string.IsNullOrEmpty(i.Key))
                .Select(JiraIssueContext.FromDto)
                .ToList();

            // Keyed by ExternalId so overlapping fetch windows collapse deterministically before the engine.
            var signals = new Dictionary<string, ActivitySignal>(StringComparer.Ordinal);

            foreach (var context in contexts)
            {
                await AddWorklogSignalsAsync(signals, context, accountId, siteUrl, container, cancellationToken);
                await AddCommentSignalsAsync(signals, context, accountId, siteUrl, container, cancellationToken);
            }

            await AddChangeSignalsAsync(signals, contexts, accountId, siteUrl, container, cancellationToken);

            IReadOnlyList<ActivitySignal> result = signals.Values.ToList();
            var errorOr = result.ToErrorOr();
            return errorOr;
        }
        catch (JiraApiException ex)
        {
            return MapError(ex);
        }
    }

    private async Task AddWorklogSignalsAsync(Dictionary<string, ActivitySignal> signals, JiraIssueContext context, string accountId, string siteUrl, ActivityFetchContainer window, CancellationToken cancellationToken)
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

            var startedUtc = started.UtcDateTime;
            if (startedUtc < window.From || startedUtc >= window.To)
            {
                continue;
            }

            var signal = JiraActivityMapper.ToWorklogSignal(context, worklog, siteUrl);
            signals[signal.ExternalId] = signal;
        }
    }

    private async Task AddCommentSignalsAsync(Dictionary<string, ActivitySignal> signals, JiraIssueContext context, string accountId, string siteUrl, ActivityFetchContainer window, CancellationToken cancellationToken)
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

            var createdUtc = created.UtcDateTime;
            if (createdUtc < window.From || createdUtc >= window.To)
            {
                continue;
            }

            var signal = JiraActivityMapper.ToCommentSignal(context, comment, siteUrl);
            signals[signal.ExternalId] = signal;
        }
    }

    private async Task AddChangeSignalsAsync(Dictionary<string, ActivitySignal> signals, IReadOnlyList<JiraIssueContext> contexts, string accountId, string siteUrl, ActivityFetchContainer window, CancellationToken cancellationToken)
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
                AddHistorySignals(signals, context, history, accountId, siteUrl, window);
            }
        }
    }

    private static void AddHistorySignals(Dictionary<string, ActivitySignal> signals, JiraIssueContext context, JiraChangelogDto history, string accountId, string siteUrl, ActivityFetchContainer window)
    {
        if (!IsAuthor(history.Author, accountId) || string.IsNullOrEmpty(history.Id) || history.Items is null)
        {
            return;
        }

        if (history.Created is not { } created)
        {
            return;
        }

        var createdUtc = created.UtcDateTime;
        if (createdUtc < window.From || createdUtc >= window.To)
        {
            return;
        }

        for (var index = 0; index < history.Items.Count; index++)
        {
            var item = history.Items[index];
            var signal = JiraActivityMapper.ToChangeSignal(context, history, item, index, siteUrl);
            signals[signal.ExternalId] = signal;
        }
    }

    private static bool IsAuthor(JiraUserDto? author, string accountId)
    {
        return author?.AccountId is { } id && string.Equals(id, accountId, StringComparison.Ordinal);
    }

    private static Error MapError(JiraApiException ex)
    {
        return ex.Kind switch
        {
            JiraApiErrorKind.Auth => Error.Unauthorized(description: ex.Message),
            _ => Error.Failure(description: ex.Message),
        };
    }
}
