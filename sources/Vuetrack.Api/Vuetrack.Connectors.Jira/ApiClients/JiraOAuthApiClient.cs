using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Duende.IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Connectors.Jira.Contracts;
using Vuetrack.Connectors.Jira.Configuration;
using Vuetrack.Connectors.Jira.Exceptions;

namespace Vuetrack.Connectors.Jira.ApiClients;

[Inject]
public class JiraOAuthApiClient : IJiraOAuthApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public JiraOAuthApiClient(HttpClient httpClient, IOptions<JiraOptions> options, ILogger<JiraOAuthApiClient> logger)
    {
        Options = options.Value;
        Logger = logger;
        httpClient.Timeout = TimeSpan.FromSeconds(Options.TimeoutSeconds);
        HttpClient = httpClient;
    }

    private HttpClient HttpClient { get; }

    private JiraOptions Options { get; }

    private ILogger<JiraOAuthApiClient> Logger { get; }

    private string TokenEndpoint => $"{Options.IdentityBaseUrl.TrimEnd('/')}/oauth/token";

    public string BuildAuthorizationUrl(string state, string redirectUri)
    {
        var request = new RequestUrl($"{Options.IdentityBaseUrl.TrimEnd('/')}/authorize");
        return request.CreateAuthorizeUrl(
            clientId: Options.ClientId,
            responseType: "code",
            scope: Options.Scopes,
            redirectUri: redirectUri,
            state: state,
            prompt: "consent",
            extra: new Parameters { { "audience", "api.atlassian.com" } });
    }

    public async Task<JiraTokenResponse> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken)
    {
        var response = await HttpClient.RequestAuthorizationCodeTokenAsync(
            new AuthorizationCodeTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = Options.ClientId,
                ClientSecret = Options.ClientSecret,
                ClientCredentialStyle = ClientCredentialStyle.PostBody,
                Code = code,
                RedirectUri = redirectUri,
            },
            cancellationToken);

        return MapToken(response);
    }

    public async Task<JiraTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var response = await HttpClient.RequestRefreshTokenAsync(
            new RefreshTokenRequest
            {
                Address = TokenEndpoint,
                ClientId = Options.ClientId,
                ClientSecret = Options.ClientSecret,
                ClientCredentialStyle = ClientCredentialStyle.PostBody,
                RefreshToken = refreshToken,
            },
            cancellationToken);

        return MapToken(response);
    }

    public async Task<IReadOnlyList<JiraAccessibleResourceResponse>> GetAccessibleResourcesAsync(string accessToken, CancellationToken cancellationToken)
    {
        var uri = $"{Options.ApiBaseUrl.TrimEnd('/')}/oauth/token/accessible-resources";
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            Logger.LogWarning("Jira accessible-resources returned {StatusCode}", (int)response.StatusCode);
            throw new JiraApiException(JiraApiErrorKind.Auth, $"Could not resolve accessible Jira sites ({(int)response.StatusCode}).");
        }

        var resources = await response.Content.ReadFromJsonAsync<List<JiraAccessibleResourceResponse>>(JsonOptions, cancellationToken);
        return resources ?? [];
    }

    private JiraTokenResponse MapToken(TokenResponse response)
    {
        if (response.IsError)
        {
            Logger.LogWarning("Jira token endpoint failed: {Error} ({StatusCode})", response.Error, (int)response.HttpStatusCode);
            throw new JiraApiException(JiraApiErrorKind.Auth, $"Jira token request failed ({response.Error ?? response.HttpStatusCode.ToString()}).");
        }

        if (string.IsNullOrEmpty(response.AccessToken))
        {
            throw new JiraApiException(JiraApiErrorKind.Transport, "Jira token response was empty.");
        }

        return new JiraTokenResponse
        {
            AccessToken = response.AccessToken,
            RefreshToken = response.RefreshToken,
            ExpiresInSeconds = response.ExpiresIn,
            Scope = response.Scope,
            TokenType = response.TokenType,
        };
    }
}

public interface IJiraOAuthApiClient
{
    string BuildAuthorizationUrl(string state, string redirectUri);

    Task<JiraTokenResponse> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken);

    Task<JiraTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task<IReadOnlyList<JiraAccessibleResourceResponse>> GetAccessibleResourcesAsync(string accessToken, CancellationToken cancellationToken);
}
