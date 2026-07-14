using Duende.IdentityModel.Client;
using Microsoft.Extensions.Logging;

namespace Vuetrack.OAuth;

public abstract class OAuthApiClientBase(HttpClient httpClient, ILogger logger) : IOAuthApiClientBase
{
    protected HttpClient HttpClient { get; } = httpClient;

    private ILogger Logger { get; } = logger;

    protected abstract string ProviderName { get; }

    protected abstract string AuthorizeEndpoint { get; }

    protected abstract string TokenEndpoint { get; }

    protected abstract string ClientId { get; }

    protected abstract string ClientSecret { get; }

    protected abstract string Scopes { get; }

    protected virtual string? AuthorizePrompt => null;

    protected virtual Parameters? ExtraAuthorizeParameters => null;

    public string BuildAuthorizationUrl(string state, string redirectUri)
    {
        var request = new RequestUrl(AuthorizeEndpoint);
        return request.CreateAuthorizeUrl(
            clientId: ClientId,
            responseType: "code",
            scope: Scopes,
            redirectUri: redirectUri,
            state: state,
            prompt: AuthorizePrompt,
            extra: ExtraAuthorizeParameters);
    }

    public async Task<OAuthTokenResponse> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken)
    {
        var tokenRequest = new AuthorizationCodeTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = ClientId,
            ClientSecret = ClientSecret,
            ClientCredentialStyle = ClientCredentialStyle.PostBody,
            Code = code,
            RedirectUri = redirectUri,
        };

        var response = await HttpClient.RequestAuthorizationCodeTokenAsync(tokenRequest, cancellationToken);
        return MapToken(response);
    }

    public async Task<OAuthTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenRequest = new RefreshTokenRequest
        {
            Address = TokenEndpoint,
            ClientId = ClientId,
            ClientSecret = ClientSecret,
            ClientCredentialStyle = ClientCredentialStyle.PostBody,
            RefreshToken = refreshToken,
        };

        var response = await HttpClient.RequestRefreshTokenAsync(tokenRequest, cancellationToken);
        return MapToken(response);
    }

    private OAuthTokenResponse MapToken(TokenResponse response)
    {
        if (response.IsError)
        {
            Logger.LogWarning("{Provider} token endpoint failed: {Error} ({StatusCode})", ProviderName, response.Error, (int)response.HttpStatusCode);
            var reason = response.Error ?? response.HttpStatusCode.ToString();
            throw new InvalidOperationException($"{ProviderName} token request failed ({reason}).");
        }

        if (string.IsNullOrEmpty(response.AccessToken))
        {
            throw new InvalidOperationException($"{ProviderName} token response was empty.");
        }

        return new OAuthTokenResponse
        {
            AccessToken = response.AccessToken,
            RefreshToken = response.RefreshToken,
            ExpiresInSeconds = response.ExpiresIn,
            Scope = response.Scope,
            TokenType = response.TokenType,
        };
    }
}


public interface IOAuthApiClientBase
{
    string BuildAuthorizationUrl(string state, string redirectUri);

    Task<OAuthTokenResponse> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken);

    Task<OAuthTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
}
