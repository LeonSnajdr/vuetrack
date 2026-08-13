using ErrorOr;
using Microsoft.Extensions.Logging;
using Vuetrack.Api.Features.Integrations.Contracts;

namespace Vuetrack.Api.Features.Integrations.Connections;

public abstract class IntegrationConnectionServiceBase(
    IOAuthApiClientBase oauthClient,
    IConnectionRepository repository,
    IConnectionContextFactory contextFactory,
    IConnectionSecretProtector secretProtector,
    IIntegrationRegistry registry,
    ILogger logger)
    : IIntegrationConnectionService
{
    protected IOAuthApiClientBase OAuthClient { get; } = oauthClient;

    protected ILogger Logger { get; } = logger;

    private IConnectionRepository Repository { get; } = repository;

    private IConnectionContextFactory ContextFactory { get; } = contextFactory;

    private IConnectionSecretProtector SecretProtector { get; } = secretProtector;

    private IIntegrationRegistry Registry { get; } = registry;

    public abstract IntegrationKey Key { get; }

    public OAuthAuthorizeContract BuildAuthorization(string redirectUri)
    {
        var state = Guid.NewGuid().ToString("N");
        var url = OAuthClient.BuildAuthorizationUrl(state, redirectUri);

        return new OAuthAuthorizeContract(url, state);
    }

    public async Task<OAuthStatusContract> GetStatusAsync(string userId, CancellationToken cancellationToken)
    {
        var connection = await Repository.GetAsync(userId, Key, cancellationToken);
        if (connection is null)
        {
            return new OAuthStatusContract(false, false);
        }

        var healthy = await CheckHealthAsync(userId, cancellationToken);

        return new OAuthStatusContract(true, healthy);
    }

    public async Task<ErrorOr<OAuthConnectContract>> ConnectAsync(string userId, OAuthConnectCreateContract request, CancellationToken cancellationToken)
    {
        try
        {
            var token = await OAuthClient.ExchangeCodeAsync(request.Code, request.RedirectUri, cancellationToken);

            var established = await EstablishAsync(token, cancellationToken);
            if (established.IsError)
            {
                return established.Errors;
            }

            await PersistAsync(userId, token, established.Value, cancellationToken);

            return new OAuthConnectContract(true);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "{Integration} connect failed", Key);
            return Error.Unexpected();
        }
    }

    public async Task DisconnectAsync(string userId, CancellationToken cancellationToken)
    {
        await Repository.DeleteAsync(userId, Key, cancellationToken);
        await ContextFactory.EvictAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Probes the freshly issued token and returns the connection attributes to persist.
    /// A failure here means the credentials are unusable, so nothing is stored.
    /// </summary>
    protected abstract Task<ErrorOr<Dictionary<string, string>>> EstablishAsync(OAuthTokenResponse token, CancellationToken cancellationToken);

    private async Task<bool> CheckHealthAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var integration = Registry.Resolve<IIntegration>(Key);
            if (integration is null)
            {
                return false;
            }

            var validation = await integration.ValidateAsync(userId, cancellationToken);

            return !validation.IsError;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogWarning(ex, "{Integration} status health check failed for user {UserId}", Key, userId);
            return false;
        }
    }

    private async Task PersistAsync(string userId, OAuthTokenResponse token, Dictionary<string, string> attributes, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(token.RefreshToken))
        {
            Logger.LogWarning("{Integration} token response had no refresh token; connection not persisted", Key);
            return;
        }

        var encryptedRefreshToken = SecretProtector.Protect(Key, token.RefreshToken);

        await Repository.UpsertAsync(userId, Key, encryptedRefreshToken, attributes, cancellationToken);
        await ContextFactory.EvictAsync(userId, cancellationToken);
    }
}

public interface IIntegrationConnectionService
{
    IntegrationKey Key { get; }

    OAuthAuthorizeContract BuildAuthorization(string redirectUri);

    Task<OAuthStatusContract> GetStatusAsync(string userId, CancellationToken cancellationToken);

    Task<ErrorOr<OAuthConnectContract>> ConnectAsync(string userId, OAuthConnectCreateContract request, CancellationToken cancellationToken);

    Task DisconnectAsync(string userId, CancellationToken cancellationToken);
}
