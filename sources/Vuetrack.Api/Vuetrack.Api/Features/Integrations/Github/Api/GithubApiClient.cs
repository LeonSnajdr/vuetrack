using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Connections;
using Vuetrack.Api.Features.Integrations.Github.Api;
using Vuetrack.Api.Features.Integrations.Github.Connection;
using Vuetrack.Api.Features.Integrations.Github.OAuth;

namespace Vuetrack.Api.Features.Integrations.Github.Api;

public class GithubApiClient(HttpClient httpClient, IOptions<GithubOptions> options, ILogger<GithubApiClient> logger)
    : IntegrationApiClientBase(httpClient, SerializerOptions, logger), IGithubApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private IOptions<GithubOptions> Options { get; } = options;

    protected override IntegrationKey Key => IntegrationKey.Github;

    public async Task<string> GetAuthenticatedLoginAsync(GithubConnectionContext context, CancellationToken cancellationToken)
    {
        var me = await GetAsync<GithubUserResponse>(context, "user", cancellationToken);
        return me.Login;
    }

    public async Task<IReadOnlyList<GithubCommitItemResponse>> SearchCommitsAsync(GithubConnectionContext context, string login, DateTime from, DateTime to, CancellationToken cancellationToken)
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
            var response = await GetAsync<GithubCommitSearchResponse>(context, path, cancellationToken);

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

    private async Task<T> GetAsync<T>(GithubConnectionContext context, string path, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(context, HttpMethod.Get, path);
        var value = await SendAsync<T>(request, cancellationToken);
        return value;
    }

    private static HttpRequestMessage BuildRequest(GithubConnectionContext context, HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
        request.Headers.UserAgent.ParseAdd("Vuetrack");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return request;
    }

    protected override Exception BuildException(bool isAuthFailure, string message)
    {
        var kind = isAuthFailure ? GithubApiErrorKind.Auth : GithubApiErrorKind.Transport;

        return new GithubApiException(kind, message);
    }

    private static string IsoDate(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

public interface IGithubApiClient
{
    Task<string> GetAuthenticatedLoginAsync(GithubConnectionContext context, CancellationToken cancellationToken);

    Task<IReadOnlyList<GithubCommitItemResponse>> SearchCommitsAsync(GithubConnectionContext context, string login, DateTime from, DateTime to, CancellationToken cancellationToken);
}
