using Microsoft.Extensions.Logging;
using Vuetrack.OAuth.Contractrs;

namespace Vuetrack.OAuth;

public abstract class OAuthConnectionServiceBase<TModel>(
    IOAuthApiClientBase oauthClient,
    IOAuthConnectionRepository<TModel> repository,
    IOAuthConnectionContextFactory contextFactory,
    IOAuthSecretProtectorBase secretProtector,
    ILogger logger)
    where TModel : OAuthConnectionModel
{
    protected IOAuthApiClientBase OAuthClient { get; } = oauthClient;

    protected ILogger Logger { get; } = logger;

    private IOAuthConnectionRepository<TModel> Repository { get; } = repository;

    private IOAuthConnectionContextFactory ContextFactory { get; } = contextFactory;

    private IOAuthSecretProtectorBase SecretProtector { get; } = secretProtector;

    protected abstract string ProviderName { get; }

    public OAuthAuthorizeContract BuildAuthorization(string redirectUri)
    {
        var state = Guid.NewGuid().ToString("N");
        var url = OAuthClient.BuildAuthorizationUrl(state, redirectUri);
        return new OAuthAuthorizeContract(url, state);
    }

    public async Task<OAuthStatusContract> GetStatusAsync(string userId, CancellationToken cancellationToken)
    {
        var connection = await Repository.GetByUserId(userId);
        if (connection is not { Enabled: true })
        {
            return new OAuthStatusContract(false, false);
        }

        var healthy = await CheckHealthAsync(userId, cancellationToken);
        return new OAuthStatusContract(true, healthy);
    }

    public async Task DisconnectAsync(string userId)
    {
        var connection = await Repository.GetByUserId(userId);
        if (connection is not null)
        {
            await Repository.Delete(connection);
        }

        ContextFactory.Evict(userId);
    }

    protected abstract Task<bool> CheckHealthAsync(string userId, CancellationToken cancellationToken);

    protected string? ProtectRefreshToken(OAuthTokenResponse token)
    {
        if (string.IsNullOrEmpty(token.RefreshToken))
        {
            Logger.LogWarning("{Provider} token response had no refresh token; connection not persisted", ProviderName);
            return null;
        }

        return SecretProtector.Protect(token.RefreshToken);
    }
}

public interface IOAuthConnectionContextFactory
{
    void Evict(string userId);
}

public interface IOAuthConnectionContextFactory<TContainer> : IOAuthConnectionContextFactory
{
    Task<TContainer?> CreateAsync(string userId, CancellationToken cancellationToken);
}
