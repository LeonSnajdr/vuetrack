using ZiggyCreatures.Caching.Fusion;

namespace Vuetrack.Api.Features.Integrations.Connections;

public abstract class ConnectionContextFactory<TContext>(
    IFusionCache cache,
    IConnectionRepository repository,
    IOAuthApiClientBase oauthClient,
    IConnectionSecretProtector secretProtector)
    : IConnectionContextFactory<TContext>
    where TContext : class
{
    // Refresh a little early so a token never expires mid-request.
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromSeconds(60);

    private IFusionCache Cache { get; } = cache;

    private IConnectionRepository Repository { get; } = repository;

    private IOAuthApiClientBase OAuthClient { get; } = oauthClient;

    private IConnectionSecretProtector SecretProtector { get; } = secretProtector;

    protected abstract IntegrationKey Key { get; }

    public async Task<TContext?> CreateAsync(string userId, CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey(userId);

        var cached = await Cache.TryGetAsync<TContext>(cacheKey, token: cancellationToken);
        if (cached.HasValue)
        {
            return cached.Value;
        }

        var connection = await Repository.GetAsync(userId, Key, cancellationToken);
        if (connection is null)
        {
            return null;
        }

        var refreshToken = SecretProtector.Unprotect(Key, connection.EncryptedRefreshToken);
        var token = await OAuthClient.RefreshAsync(refreshToken, cancellationToken);

        if (!string.IsNullOrEmpty(token.RefreshToken) && token.RefreshToken != refreshToken)
        {
            var rotatedRefreshToken = SecretProtector.Protect(Key, token.RefreshToken);
            await Repository.SetRefreshTokenAsync(userId, Key, rotatedRefreshToken, cancellationToken);
        }

        var context = BuildContext(userId, token.AccessToken, connection);

        var lifetime = TimeSpan.FromSeconds(token.ExpiresInSeconds) - ExpiryBuffer;
        if (lifetime > TimeSpan.Zero)
        {
            await Cache.SetAsync(cacheKey, context, lifetime, token: cancellationToken);
        }

        return context;
    }

    public async Task EvictAsync(string userId, CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey(userId);

        await Cache.RemoveAsync(cacheKey, token: cancellationToken);
    }

    protected abstract TContext BuildContext(string userId, string accessToken, ConnectionModel connection);

    private string BuildCacheKey(string userId) => $"connection:{Key}:{userId}";
}

public interface IConnectionContextFactory
{
    Task EvictAsync(string userId, CancellationToken cancellationToken);
}

public interface IConnectionContextFactory<TContext> : IConnectionContextFactory
{
    Task<TContext?> CreateAsync(string userId, CancellationToken cancellationToken);
}
