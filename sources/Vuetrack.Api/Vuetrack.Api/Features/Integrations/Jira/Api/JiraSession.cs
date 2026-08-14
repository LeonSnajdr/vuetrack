using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Connections;

namespace Vuetrack.Api.Features.Integrations.Jira.Api;

public class JiraSession(
    HttpClient httpClient,
    IOptions<JiraOptions> options,
    ILogger<JiraSession> logger,
    string accessToken,
    string cloudId,
    string siteUrl)
    : IntegrationApiClientBase(httpClient, SerializerOptions, logger), IJiraSession
{
    private static readonly JsonSerializerOptions SerializerOptions = BuildJsonOptions();

    private IOptions<JiraOptions> Options { get; } = options;

    private string AccessToken { get; } = accessToken;

    private string CloudId { get; } = cloudId;

    public string SiteUrl { get; } = siteUrl;

    protected override IntegrationKey Key => IntegrationKey.Jira;

    public async Task<string> GetMyAccountIdAsync(CancellationToken cancellationToken)
    {
        var me = await GetAsync<JiraUserResponse>("myself", cancellationToken);
        return me.AccountId ?? string.Empty;
    }

    public async Task<JiraSearchIssueResponse> GetIssueAsync(string issueKey, CancellationToken cancellationToken)
    {
        var path = $"issue/{Uri.EscapeDataString(issueKey)}?fields=summary,issuetype,status";
        var issue = await GetAsync<JiraSearchIssueResponse>(path, cancellationToken);
        return issue;
    }

    public async Task<IReadOnlyList<JiraSearchIssueResponse>> SearchCandidateIssuesAsync(DateRange range, CancellationToken cancellationToken)
    {
        var fromIso = IsoDateTime(range.From);
        var toIso = IsoDateTime(range.To);
        var jql = $"(worklogAuthor = currentUser() OR assignee was currentUser() OR status changed by currentUser()) AND updated >= \"{fromIso}\" AND updated <= \"{toIso}\" ORDER BY updated ASC";

        var request = new JiraSearchRequest
        {
            Jql = jql,
            Fields = ["summary", "issuetype", "status", "parent"],
            MaxResults = 5000,
        };

        var response = await PostAsync<JiraSearchResponse>("search/jql", request, cancellationToken);
        return response.Issues ?? [];
    }

    public async Task<IReadOnlyList<JiraWorklogResponse>> GetWorklogsAsync(string issueKey, DateTime startedAfter, CancellationToken cancellationToken)
    {
        var startedAfterMs = new DateTimeOffset(startedAfter, TimeSpan.Zero).ToUnixTimeMilliseconds();
        var entries = new List<JiraWorklogResponse>();
        var startAt = 0;

        for (var page = 0; page < Options.Value.MaxPages; page++)
        {
            var path = $"issue/{Uri.EscapeDataString(issueKey)}/worklog?startedAfter={startedAfterMs}&startAt={startAt}&maxResults={Options.Value.PageSize}";
            var response = await GetAsync<JiraWorklogPageResponse>(path, cancellationToken);

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

    public async Task<IReadOnlyList<JiraCommentResponse>> GetCommentsAsync(string issueKey, CancellationToken cancellationToken)
    {
        var comments = new List<JiraCommentResponse>();
        var startAt = 0;

        for (var page = 0; page < Options.Value.MaxPages; page++)
        {
            var path = $"issue/{Uri.EscapeDataString(issueKey)}/comment?startAt={startAt}&maxResults={Options.Value.PageSize}";
            var response = await GetAsync<JiraCommentPageResponse>(path, cancellationToken);

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

    public async Task<IReadOnlyList<JiraIssueChangeLogResponse>> GetChangelogsAsync(IReadOnlyList<string> issueIdsOrKeys, CancellationToken cancellationToken)
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

            var response = await PostAsync<JiraBulkChangelogResponse>("changelog/bulkfetch", request, cancellationToken);
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

    protected override Exception BuildException(bool isAuthFailure, string message)
    {
        var kind = isAuthFailure ? JiraApiErrorKind.Auth : JiraApiErrorKind.Transport;

        return new JiraApiException(kind, message);
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Get, path);
        var value = await SendAsync<T>(request, cancellationToken);
        return value;
    }

    private async Task<T> PostAsync<T>(string path, object body, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Post, path);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        var value = await SendAsync<T>(request, cancellationToken);
        return value;
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string path)
    {
        var uri = $"ex/jira/{CloudId}/rest/api/3/{path}";

        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
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

public interface IJiraSession
{
    string SiteUrl { get; }

    Task<string> GetMyAccountIdAsync(CancellationToken cancellationToken);

    Task<JiraSearchIssueResponse> GetIssueAsync(string issueKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<JiraSearchIssueResponse>> SearchCandidateIssuesAsync(DateRange range, CancellationToken cancellationToken);

    Task<IReadOnlyList<JiraWorklogResponse>> GetWorklogsAsync(string issueKey, DateTime startedAfter, CancellationToken cancellationToken);

    Task<IReadOnlyList<JiraCommentResponse>> GetCommentsAsync(string issueKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<JiraIssueChangeLogResponse>> GetChangelogsAsync(IReadOnlyList<string> issueIdsOrKeys, CancellationToken cancellationToken);
}
