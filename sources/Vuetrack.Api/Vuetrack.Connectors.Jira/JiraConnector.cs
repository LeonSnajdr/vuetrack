using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Jira.Activity;
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

    public async Task<ConnectorValidationResult> ValidateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var accountId = await Client.GetMyAccountIdAsync(cancellationToken);

            if (string.IsNullOrEmpty(accountId))
            {
                return new ConnectorValidationInvalid(["Jira did not return an account for these credentials."]);
            }

            return new ConnectorValidationValid();
        }
        catch (JiraApiException ex)
        {
            return new ConnectorValidationInvalid([ex.Message]);
        }
    }

    public async Task<ActivityFetchResult> FetchAsync(ActivityFetchContainer container, CancellationToken cancellationToken)
    {
        try
        {
            var accountId = await Client.GetMyAccountIdAsync(cancellationToken);
            var worklogs = await Client.GetWorklogEntriesAsync(accountId, container.From, container.To, cancellationToken);
            var issues = await Client.GetIssueActivityAsync(container.From, container.To, cancellationToken);

            var siteUrl = Accessor.Current?.SiteUrl ?? string.Empty;
            var mapperContext = new JiraMapperContext(Key, siteUrl);
            var signals = MergeSignals(worklogs, issues, mapperContext);
            return new ActivityFetchSuccess(signals);
        }
        catch (JiraApiException ex)
        {
            return ex.Kind switch
            {
                JiraApiErrorKind.Auth => new ActivityFetchAuthFailed(ex.Message),
                JiraApiErrorKind.RateLimited => new ActivityFetchRateLimited(ex.RetryAfter ?? TimeSpan.FromSeconds(60)),
                _ => new ActivityFetchConnectorError(ex.Message),
            };
        }
    }

    private static IReadOnlyList<ActivitySignal> MergeSignals(IReadOnlyList<JiraWorklogContainer> worklogs, IReadOnlyList<JiraIssueActivityContainer> issues, JiraMapperContext context)
    {
        var byExternalId = new Dictionary<string, ActivitySignal>();

        var issueKeysWithWorklog = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var worklog in worklogs)
        {
            issueKeysWithWorklog.Add(worklog.IssueKey);
            var signal = worklog.ToActivitySignal(context);
            byExternalId[signal.ExternalId] = signal;
        }

        foreach (var issue in issues)
        {
            if (issueKeysWithWorklog.Contains(issue.IssueKey))
            {
                continue;
            }

            var signal = issue.ToActivitySignal(context);
            byExternalId[signal.ExternalId] = signal;
        }

        return byExternalId.Values.ToList();
    }
}
