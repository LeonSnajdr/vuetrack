using Duende.IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Samhammer.DependencyInjection.Attributes;

namespace Vuetrack.Backends.Timetracking.OAuth;

[Inject]
public class TimetrackingOAuthApiClient(HttpClient httpClient, IOptions<TimetrackingOptions> options, ILogger<TimetrackingOAuthApiClient> logger) : ITimetrackingOAuthApiClient
{
    private HttpClient HttpClient { get; } = httpClient;

    private IOptions<TimetrackingOptions> Options { get; } = options;

    private ILogger<TimetrackingOAuthApiClient> Logger { get; } = logger;

    public string BuildAuthorizationUrl(string state, string redirectUri)
    {
        var request = new RequestUrl(Options.Value.AuthorizeEndpoint);
        return request.CreateAuthorizeUrl(
            clientId: Options.Value.ClientId,
            responseType: "code",
            scope: Options.Value.Scopes,
            redirectUri: redirectUri,
            state: state);
    }

    public async Task<TimetrackingTokenResponse> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken)
    {
        var response = await HttpClient.RequestAuthorizationCodeTokenAsync(
            new AuthorizationCodeTokenRequest
            {
                Address = Options.Value.TokenEndpoint,
                ClientId = Options.Value.ClientId,
                ClientSecret = Options.Value.ClientSecret,
                ClientCredentialStyle = ClientCredentialStyle.PostBody,
                Code = code,
                RedirectUri = redirectUri,
            },
            cancellationToken);

        return MapToken(response);
    }

    public async Task<TimetrackingTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var response = await HttpClient.RequestRefreshTokenAsync(
            new RefreshTokenRequest
            {
                Address = Options.Value.TokenEndpoint,
                ClientId = Options.Value.ClientId,
                ClientSecret = Options.Value.ClientSecret,
                ClientCredentialStyle = ClientCredentialStyle.PostBody,
                RefreshToken = refreshToken,
            },
            cancellationToken);

        return MapToken(response);
    }

    private TimetrackingTokenResponse MapToken(TokenResponse response)
    {
        if (response.IsError)
        {
            Logger.LogWarning("Timetracking token endpoint failed: {Error} ({StatusCode})", response.Error, (int)response.HttpStatusCode);
            throw new InvalidOperationException($"Timetracking token request failed ({response.Error ?? response.HttpStatusCode.ToString()}).");
        }

        if (string.IsNullOrEmpty(response.AccessToken))
        {
            throw new InvalidOperationException("Timetracking token response was empty.");
        }

        return new TimetrackingTokenResponse
        {
            AccessToken = response.AccessToken,
            RefreshToken = response.RefreshToken,
            ExpiresInSeconds = response.ExpiresIn,
            Scope = response.Scope,
            TokenType = response.TokenType,
        };
    }
}

public interface ITimetrackingOAuthApiClient
{
    string BuildAuthorizationUrl(string state, string redirectUri);

    Task<TimetrackingTokenResponse> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken);

    Task<TimetrackingTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
}
