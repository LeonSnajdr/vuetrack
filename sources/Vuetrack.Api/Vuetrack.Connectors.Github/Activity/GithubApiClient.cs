using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vuetrack.Connectors.Github.Activity.Api;
using Vuetrack.Connectors.Github.Connection;
using Vuetrack.Connectors.Github.OAuth;

namespace Vuetrack.Connectors.Github.Activity;

public class GithubApiClient(HttpClient httpClient, IGithubConnectionAccessor accessor, IOptions<GithubOptions> options, ILogger<GithubApiClient> logger) : IGithubApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private HttpClient HttpClient { get; } = httpClient;

    private IGithubConnectionAccessor Accessor { get; } = accessor;

    private IOptions<GithubOptions> Options { get; } = options;

    private ILogger<GithubApiClient> Logger { get; } = logger;

    public async Task<string> GetAuthenticatedLoginAsync(CancellationToken cancellationToken)
    {
        var me = await GetAsync<GithubUserResponse>("user", cancellationToken);
        return me.Login;
    }

    public async Task<IReadOnlyList<GithubCommitItemResponse>> SearchCommitsAsync(string login, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var fromIso = IsoDate(from);
        var toIso = IsoDate(to);
        var query = $"author:{login} author-date:{fromIso}..{toIso}";
        var encodedQuery = Uri.EscapeDataString(query);
        var pageSize = Math.Clamp(Options.Value.PageSize, 1, 100);

        var items = new List<GithubCommitItemResponse>();

        for (var page = 1; page <= Options.Value.MaxPages; page++)
        {
            var path = $"search/commits?q={encodedQuery}&per_page={pageSize}&page={page}";
            var response = await GetAsync<GithubCommitSearchResponse>(path, cancellationToken);

            var pageItems = response.Items;
            if (pageItems is not { Count: > 0 })
            {
                break;
            }

            items.AddRange(pageItems);

            if (pageItems.Count < pageSize || items.Count >= response.TotalCount)
            {
                break;
            }
        }

        return items;
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Get, path);
        var value = await SendAsync<T>(request, cancellationToken);
        return value;
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string path)
    {
        var connection = Accessor.Current ?? throw new GithubApiException(GithubApiErrorKind.Auth, "No active GitHub connection for this request.");

        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);
        request.Headers.UserAgent.ParseAdd("Vuetrack");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return request;
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await HttpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new GithubApiException(GithubApiErrorKind.Transport, $"GitHub request failed: {ex.Message}");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var error = ToException(response);
                throw error;
            }

            var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            if (value is null)
            {
                throw new GithubApiException(GithubApiErrorKind.Transport, "GitHub returned an empty response.");
            }

            return value;
        }
    }

    private GithubApiException ToException(HttpResponseMessage response)
    {
        Logger.LogWarning("GitHub API returned {StatusCode}", (int)response.StatusCode);

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new GithubApiException(GithubApiErrorKind.Auth, $"GitHub rejected the credentials ({(int)response.StatusCode})."),
            _ => new GithubApiException(GithubApiErrorKind.Transport, $"GitHub request failed ({(int)response.StatusCode})."),
        };
    }

    private static string IsoDate(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

public interface IGithubApiClient
{
    Task<string> GetAuthenticatedLoginAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<GithubCommitItemResponse>> SearchCommitsAsync(string login, DateTime from, DateTime to, CancellationToken cancellationToken);
}
