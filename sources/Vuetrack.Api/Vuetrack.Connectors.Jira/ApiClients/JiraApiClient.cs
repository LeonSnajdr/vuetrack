using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vuetrack.Connectors.Jira.Containers;
using Vuetrack.Connectors.Jira.Exceptions;
using Vuetrack.Connectors.Jira.Configuration;
using Vuetrack.Connectors.Jira.Internal;
using Vuetrack.Connectors.Jira.Services;

namespace Vuetrack.Connectors.Jira.ApiClients;

public class JiraApiClient : IJiraApiClient
{
    public JiraApiClient(HttpClient httpClient, IJiraConnectionAccessor accessor, IOptions<JiraOptions> options, ILogger<JiraApiClient> logger)
    {
        Options = options.Value;
        Logger = logger;
        Accessor = accessor;
        httpClient.Timeout = TimeSpan.FromSeconds(Options.TimeoutSeconds);
        HttpClient = httpClient;
    }

    private HttpClient HttpClient { get; }

    private IJiraConnectionAccessor Accessor { get; }

    private JiraOptions Options { get; }

    private ILogger<JiraApiClient> Logger { get; }

    public async Task<string> GetMyAccountIdAsync(CancellationToken cancellationToken)
    {
        using var document = await GetJsonAsync("myself", cancellationToken);
        return document.RootElement.TryGetProperty("accountId", out var accountId)
            ? accountId.GetString() ?? string.Empty
            : string.Empty;
    }

    public async Task<IReadOnlyList<JiraWorklogContainer>> GetWorklogEntriesAsync(
        string accountId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var jql = $"worklogAuthor = currentUser() AND worklogDate >= \"{IsoDate(from)}\" AND worklogDate <= \"{IsoDate(to)}\" ORDER BY updated DESC";
        var issues = await SearchIssuesAsync(jql, "summary,issuetype,status,project", cancellationToken);

        var startedAfterMs = from.ToUnixTimeMilliseconds();
        var entries = new List<JiraWorklogContainer>();

        foreach (var issue in issues)
        {
            var path = $"issue/{Uri.EscapeDataString(issue.Key)}/worklog?startedAfter={startedAfterMs}";
            using var document = await GetJsonAsync(path, cancellationToken);

            if (!document.RootElement.TryGetProperty("worklogs", out var worklogs) || worklogs.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var worklog in worklogs.EnumerateArray())
            {
                var authorId = worklog.GetPropertyPath("author", "accountId")?.GetString();
                if (!string.Equals(authorId, accountId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!TryGetDate(worklog, "started", out var started) || started < from || started >= to)
                {
                    continue;
                }

                var comment = worklog.TryGetProperty("comment", out var commentNode) && commentNode.ValueKind == JsonValueKind.Object
                    ? AdfTextExtractor.Extract(commentNode)
                    : null;

                entries.Add(new JiraWorklogContainer
                {
                    IssueKey = issue.Key,
                    IssueSummary = issue.Summary,
                    WorklogId = worklog.TryGetProperty("id", out var id) ? id.GetString() ?? string.Empty : string.Empty,
                    Started = started,
                    TimeSpentSeconds = worklog.TryGetProperty("timeSpentSeconds", out var secs) && secs.TryGetInt64(out var s) ? s : 0,
                    Comment = comment,
                    Project = issue.Project,
                    IssueType = issue.IssueType,
                    Status = issue.Status,
                });
            }
        }

        return entries;
    }

    public async Task<IReadOnlyList<JiraIssueActivityContainer>> GetIssueActivityAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var jql = $"(assignee was currentUser() OR status changed by currentUser()) AND updated >= \"{IsoDateTime(from)}\" AND updated <= \"{IsoDateTime(to)}\" ORDER BY updated DESC";
        var issues = await SearchIssuesAsync(jql, "summary,issuetype,status,project,updated", cancellationToken);

        return issues
            .Where(i => i.Updated is not null)
            .Select(i => new JiraIssueActivityContainer
            {
                IssueKey = i.Key,
                Summary = i.Summary,
                Updated = i.Updated!.Value,
                Project = i.Project,
                IssueType = i.IssueType,
                Status = i.Status,
            })
            .ToList();
    }

    private async Task<IReadOnlyList<SearchIssueContainer>> SearchIssuesAsync(
        string jql,
        string fields,
        CancellationToken cancellationToken)
    {
        var results = new List<SearchIssueContainer>();
        string? pageToken = null;

        for (var page = 0; page < Options.MaxPages; page++)
        {
            var path = $"search/jql?jql={Uri.EscapeDataString(jql)}&fields={Uri.EscapeDataString(fields)}&maxResults={Options.PageSize}";
            if (pageToken is not null)
            {
                path += $"&nextPageToken={Uri.EscapeDataString(pageToken)}";
            }

            using var document = await GetJsonAsync(path, cancellationToken);
            var root = document.RootElement;

            if (root.TryGetProperty("issues", out var issues) && issues.ValueKind == JsonValueKind.Array)
            {
                foreach (var issue in issues.EnumerateArray())
                {
                    results.Add(ParseSearchIssue(issue));
                }
            }

            pageToken = root.TryGetProperty("nextPageToken", out var next) && next.ValueKind == JsonValueKind.String
                ? next.GetString()
                : null;

            var isLast = root.TryGetProperty("isLast", out var last) && last.ValueKind is JsonValueKind.True;
            if (isLast || string.IsNullOrEmpty(pageToken))
            {
                break;
            }
        }

        return results;
    }

    private static SearchIssueContainer ParseSearchIssue(JsonElement issue)
    {
        var key = issue.TryGetProperty("key", out var k) ? k.GetString() ?? string.Empty : string.Empty;
        var summary = issue.GetPropertyPath("fields", "summary")?.GetString() ?? string.Empty;
        var issueType = issue.GetPropertyPath("fields", "issuetype", "name")?.GetString();
        var status = issue.GetPropertyPath("fields", "status", "name")?.GetString();
        var project = issue.GetPropertyPath("fields", "project", "key")?.GetString();

        DateTimeOffset? updated = null;
        var fields = issue.TryGetProperty("fields", out var f) ? f : default;
        if (fields.ValueKind == JsonValueKind.Object && TryGetDate(fields, "updated", out var parsed))
        {
            updated = parsed;
        }

        return new SearchIssueContainer(key, summary, issueType, status, project, updated);
    }

    private async Task<JsonDocument> GetJsonAsync(string path, CancellationToken cancellationToken)
    {
        var connection = Accessor.Current
            ?? throw new JiraApiException(JiraApiErrorKind.Auth, "No active Jira connection for this request.");

        var uri = $"{Options.ApiBaseUrl.TrimEnd('/')}/ex/jira/{connection.CloudId}/rest/api/3/{path}";

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response;
        try
        {
            response = await HttpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new JiraApiException(JiraApiErrorKind.Transport, $"Jira request failed: {ex.Message}");
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            }

            throw ToException(response);
        }
    }

    private JiraApiException ToException(HttpResponseMessage response)
    {
        Logger.LogWarning("Jira API returned {StatusCode}", (int)response.StatusCode);

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                new JiraApiException(JiraApiErrorKind.Auth, $"Jira rejected the credentials ({(int)response.StatusCode})."),
            HttpStatusCode.TooManyRequests =>
                new JiraApiException(JiraApiErrorKind.RateLimited, "Jira rate limit exceeded.", GetRetryAfter(response)),
            _ =>
                new JiraApiException(JiraApiErrorKind.Transport, $"Jira request failed ({(int)response.StatusCode})."),
        };
    }

    private static TimeSpan GetRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta)
        {
            return delta;
        }

        if (retryAfter?.Date is { } date)
        {
            var wait = date - DateTimeOffset.UtcNow;
            return wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
        }

        return TimeSpan.FromSeconds(60);
    }

    private static string IsoDate(DateTimeOffset value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string IsoDateTime(DateTimeOffset value) => value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    private static bool TryGetDate(JsonElement element, string property, out DateTimeOffset value)
    {
        value = default;
        if (!element.TryGetProperty(property, out var prop) || prop.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        return DateTimeOffset.TryParse(prop.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }

    private sealed record SearchIssueContainer(
        string Key,
        string Summary,
        string? IssueType,
        string? Status,
        string? Project,
        DateTimeOffset? Updated);
}

public interface IJiraApiClient
{
    Task<string> GetMyAccountIdAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<JiraWorklogContainer>> GetWorklogEntriesAsync(string accountId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    Task<IReadOnlyList<JiraIssueActivityContainer>> GetIssueActivityAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);
}
