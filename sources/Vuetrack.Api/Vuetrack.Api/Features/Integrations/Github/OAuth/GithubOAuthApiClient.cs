using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Connections;
using Vuetrack.Api.Features.Integrations.Github.Api;

namespace Vuetrack.Api.Features.Integrations.Github.OAuth;

[Inject]
public class GithubOAuthApiClient(HttpClient httpClient, IOptions<GithubOptions> options, ILogger<GithubOAuthApiClient> logger) : OAuthApiClientBase(httpClient, logger, options), IGithubOAuthApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private IOptions<GithubOptions> Options { get; } = options;

    private ILogger<GithubOAuthApiClient> Logger { get; } = logger;

    protected override IntegrationKey Key => IntegrationKey.Github;

    public async Task<GithubUserResponse> GetAuthenticatedUserAsync(string accessToken, CancellationToken cancellationToken)
    {
        var uri = $"{Options.Value.ApiBaseUrl.TrimEnd('/')}/user";
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd("Vuetrack");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            Logger.LogWarning("GitHub user endpoint returned {StatusCode}", (int)response.StatusCode);
            throw new GithubApiException(GithubApiErrorKind.Auth, $"Could not resolve the authenticated GitHub user ({(int)response.StatusCode}).");
        }

        var user = await response.Content.ReadFromJsonAsync<GithubUserResponse>(JsonOptions, cancellationToken);
        return user ?? new GithubUserResponse();
    }
}

public interface IGithubOAuthApiClient : IOAuthApiClientBase
{
    Task<GithubUserResponse> GetAuthenticatedUserAsync(string accessToken, CancellationToken cancellationToken);
}
