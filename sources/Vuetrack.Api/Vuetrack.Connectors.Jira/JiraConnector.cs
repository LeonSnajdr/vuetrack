using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Jira.ApiClients;
using Vuetrack.Connectors.Jira.Containers;
using Vuetrack.Connectors.Jira.Exceptions;
using Vuetrack.Connectors.Jira.Mapping;
using Vuetrack.Connectors.Jira.Services;

namespace Vuetrack.Connectors.Jira;

[InjectAs(typeof(ISuggestionConnector))]
public class JiraConnector(IJiraApiClient client, IJiraConnectionAccessor accessor) : ISuggestionConnector
{
    public const string Key = "jira";

    private IJiraApiClient Client { get; } = client;

    private IJiraConnectionAccessor Accessor { get; } = accessor;

    public ConnectorDescriptor Descriptor { get; } = new()
    {
        Key = Key,
        DisplayName = "Jira",
        Capabilities = ConnectorCapabilities.Worklogs | ConnectorCapabilities.IssueActivity | ConnectorCapabilities.OAuth,
    };

    public async Task<ValidationOutcome> ValidateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var accountId = await Client.GetMyAccountIdAsync(cancellationToken);
            return string.IsNullOrEmpty(accountId)
                ? new ValidationInvalid(["Jira did not return an account for these credentials."])
                : new ValidationValid();
        }
        catch (JiraApiException ex)
        {
            return new ValidationInvalid([ex.Message]);
        }
    }

    public async Task<FetchResult> FetchAsync(FetchContainer request, CancellationToken cancellationToken)
    {
        try
        {
            var accountId = await Client.GetMyAccountIdAsync(cancellationToken);
            var worklogs = await Client.GetWorklogEntriesAsync(accountId, request.From, request.To, cancellationToken);
            var issues = await Client.GetIssueActivityAsync(request.From, request.To, cancellationToken);

            var siteUrl = Accessor.Current?.SiteUrl ?? string.Empty;
            var mapperContext = new JiraMapperContainer(Key, siteUrl);
            var signals = MergeSignals(worklogs, issues, mapperContext);
            return new FetchSuccess(signals);
        }
        catch (JiraApiException ex)
        {
            return ex.Kind switch
            {
                JiraApiErrorKind.Auth => new FetchAuthFailed(ex.Message),
                JiraApiErrorKind.RateLimited => new FetchRateLimited(ex.RetryAfter ?? TimeSpan.FromSeconds(60)),
                _ => new FetchConnectorError(ex.Message),
            };
        }
    }

    private static IReadOnlyList<ActivitySignal> MergeSignals(IReadOnlyList<JiraWorklogContainer> worklogs, IReadOnlyList<JiraIssueActivityContainer> issues, JiraMapperContainer context)
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
