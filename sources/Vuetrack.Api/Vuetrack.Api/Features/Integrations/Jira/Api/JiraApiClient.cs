using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Connections;
using Vuetrack.Api.Features.Integrations.Jira.Api;
using Vuetrack.Api.Features.Integrations.Jira.Connection;

namespace Vuetrack.Api.Features.Integrations.Jira.Api;

public class JiraApiClient(HttpClient httpClient, IOptions<JiraOptions> options, ILogger<JiraApiClient> logger)
    : IntegrationApiClientBase(httpClient, SerializerOptions, logger), IJiraApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = BuildJsonOptions();

    private IOptions<JiraOptions> Options { get; } = options;

    protected override IntegrationKey Key => IntegrationKey.Jira;

    public async Task<string> GetMyAccountIdAsync(JiraConnectionContext context, CancellationToken cancellationToken)
    {
        var me = await GetAsync<JiraUserResponse>(context, "myself", cancellationToken);
        return me.AccountId ?? string.Empty;
    }

    public async Task<JiraSearchIssueResponse> GetIssueAsync(JiraConnectionContext context, string issueKey, CancellationToken cancellationToken)
    {
        var path = $"issue/{Uri.EscapeDataString(issueKey)}?fields=summary,issuetype,status";
        var issue = await GetAsync<JiraSearchIssueResponse>(context, path, cancellationToken);
        return issue;
    }

    public async Task<IReadOnlyList<JiraSearchIssueResponse>> SearchCandidateIssuesAsync(JiraConnectionContext context, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var fromIso = IsoDateTime(from);
        var toIso = IsoDateTime(to);
        var jql = $"(worklogAuthor = currentUser() OR assignee was currentUser() OR status changed by currentUser()) AND updated >= \"{fromIso}\" AND updated <= \"{toIso}\" ORDER BY updated ASC";

        var request = new JiraSearchRequest
        {
            Jql = jql,
            Fields = ["summary", "issuetype", "status", "parent"],
            MaxResults = 5000,
        };

        var response = await PostAsync<JiraSearchResponse>(context, "search/jql", request, cancellationToken);
        return response.Issues ?? [];
    }

    public async Task<IReadOnlyList<JiraWorklogResponse>> GetWorklogsAsync(JiraConnectionContext context, string issueKey, DateTime startedAfter, CancellationToken cancellationToken)
    {
        var startedAfterMs = new DateTimeOffset(startedAfter, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var entries = new List<JiraWorklogResponse>();
        var startAt = 0;

        for (var page = 0; page < Options.Value.MaxPages; page++)
        {
            var path = $"issue/{Uri.EscapeDataString(issueKey)}/worklog?startedAfter={startedAfterMs}&startAt={startAt}&maxResults={Options.Value.PageSize}";
            var response = await GetAsync<JiraWorklogPageResponse>(context, path, cancellationToken);

            var worklogs = response.Worklogs;
            if (worklogs is not { Count: > 0 })
            {
                break;
            }

            entries.AddRange(worklogs);
            startAt += worklogs.Count;
            if (startAt >= response.Total)
            {
                break;
            }
        }

        return entries;
    }

    public async Task<IReadOnlyList<JiraCommentResponse>> GetCommentsAsync(JiraConnectionContext context, string issueKey, CancellationToken cancellationToken)
    {
        var comments = new List<JiraCommentResponse>();
        var startAt = 0;

        for (var page = 0; page < Options.Value.MaxPages; page++)
        {
            var path = $"issue/{Uri.EscapeDataString(issueKey)}/comment?startAt={startAt}&maxResults={Options.Value.PageSize}";
            var response = await GetAsync<JiraCommentPageResponse>(context, path, cancellationToken);

            var pageComments = response.Comments;
            if (pageComments is not { Count: > 0 })
            {
                break;
            }

            comments.AddRange(pageComments);
            startAt += pageComments.Count;
            if (startAt >= response.Total)
            {
                break;
            }
        }

        return comments;
    }

    public async Task<IReadOnlyList<JiraIssueChangeLogResponse>> GetChangelogsAsync(JiraConnectionContext context, IReadOnlyList<string> issueIdsOrKeys, CancellationToken cancellationToken)
    {
        if (issueIdsOrKeys.Count == 0)
        {
            return [];
        }

        var logs = new List<JiraIssueChangeLogResponse>();
        string? pageToken = null;

        for (var page = 0; page < Options.Value.MaxPages; page++)
        {
            var request = new JiraBulkChangelogRequest
            {
                IssueIdsOrKeys = issueIdsOrKeys,
                FieldIds = ["status", "summary", "description", "priority", "assignee", "resolution", "parent", "labels"],
                MaxResults = Options.Value.PageSize,
                NextPageToken = pageToken,
            };

            var response = await PostAsync<JiraBulkChangelogResponse>(context, "changelog/bulkfetch", request, cancellationToken);
            if (response.IssueChangeLogs is { Count: > 0 })
            {
                logs.AddRange(response.IssueChangeLogs);
            }

            pageToken = response.NextPageToken;
            if (string.IsNullOrEmpty(pageToken))
            {
                break;
            }
        }

        return logs;
    }

    private async Task<T> GetAsync<T>(JiraConnectionContext context, string path, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(context, HttpMethod.Get, path);
        var value = await SendAsync<T>(request, cancellationToken);
        return value;
    }

    private async Task<T> PostAsync<T>(JiraConnectionContext context, string path, object body, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(context, HttpMethod.Post, path);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        var value = await SendAsync<T>(request, cancellationToken);
        return value;
    }

    protected override Exception BuildException(bool isAuthFailure, string message)
    {
        var kind = isAuthFailure ? JiraApiErrorKind.Auth : JiraApiErrorKind.Transport;

        return new JiraApiException(kind, message);
    }

    private static HttpRequestMessage BuildRequest(JiraConnectionContext context, HttpMethod method, string path)
    {
        var uri = $"ex/jira/{context.CloudId}/rest/api/3/{path}";

        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static string IsoDateTime(DateTime value) => value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    private static JsonSerializerOptions BuildJsonOptions()
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.Converters.Add(new JiraDateTimeConverter());
        return jsonOptions;
    }
}

public interface IJiraApiClient
{
    Task<string> GetMyAccountIdAsync(JiraConnectionContext context, CancellationToken cancellationToken);

    Task<JiraSearchIssueResponse> GetIssueAsync(JiraConnectionContext context, string issueKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<JiraSearchIssueResponse>> SearchCandidateIssuesAsync(JiraConnectionContext context, DateTime from, DateTime to, CancellationToken cancellationToken);

    Task<IReadOnlyList<JiraWorklogResponse>> GetWorklogsAsync(JiraConnectionContext context, string issueKey, DateTime startedAfter, CancellationToken cancellationToken);

    Task<IReadOnlyList<JiraCommentResponse>> GetCommentsAsync(JiraConnectionContext context, string issueKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<JiraIssueChangeLogResponse>> GetChangelogsAsync(JiraConnectionContext context, IReadOnlyList<string> issueIdsOrKeys, CancellationToken cancellationToken);
}
