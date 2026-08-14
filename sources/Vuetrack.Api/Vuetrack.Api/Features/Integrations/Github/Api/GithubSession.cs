using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Connections;
using Vuetrack.Api.Features.Integrations.Github.OAuth;

namespace Vuetrack.Api.Features.Integrations.Github.Api;

public class GithubSession(
    HttpClient httpClient,
    IOptions<GithubOptions> options,
    ILogger<GithubSession> logger,
    string accessToken,
    string login)
    : IntegrationApiClientBase(httpClient, SerializerOptions, logger), IGithubSession
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private IOptions<GithubOptions> Options { get; } = options;

    private string AccessToken { get; } = accessToken;

    public string Login { get; } = login;

    protected override IntegrationKey Key => IntegrationKey.Github;

    public async Task<string> GetAuthenticatedLoginAsync(CancellationToken cancellationToken)
    {
        var me = await GetAsync<GithubUserResponse>("user", cancellationToken);
        return me.Login;
    }

    public async Task<IReadOnlyList<GithubCommitItemResponse>> SearchCommitsAsync(DateRange range, CancellationToken cancellationToken)
    {
        var fromIso = IsoDate(range.From);
        var toIso = IsoDate(range.To);
        var query = $"author:{Login} author-date:{fromIso}..{toIso}";
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

    protected override Exception BuildException(bool isAuthFailure, string message)
    {
        var kind = isAuthFailure ? GithubApiErrorKind.Auth : GithubApiErrorKind.Transport;

        return new GithubApiException(kind, message);
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Get, path);
        var value = await SendAsync<T>(request, cancellationToken);
        return value;
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        request.Headers.UserAgent.ParseAdd("Vuetrack");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return request;
    }

    private static string IsoDate(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

public interface IGithubSession
{
    string Login { get; }

    Task<string> GetAuthenticatedLoginAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<GithubCommitItemResponse>> SearchCommitsAsync(DateRange range, CancellationToken cancellationToken);
}
