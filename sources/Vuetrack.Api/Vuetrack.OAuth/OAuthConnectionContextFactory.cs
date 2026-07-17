using ZiggyCreatures.Caching.Fusion;

namespace Vuetrack.OAuth;

public abstract class OAuthConnectionContextFactory<TContainer, TConnection>(
    IFusionCache cache,
    IOAuthConnectionRepository<TConnection> repository,
    IOAuthApiClientBase oauthClient,
    IOAuthSecretProtectorBase secretProtector)
    : IOAuthConnectionContextFactory<TContainer>
    where TContainer : class
    where TConnection : OAuthConnectionModel
{
    // Refresh a little early so a token never expires mid-request.
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromSeconds(60);

    private IFusionCache Cache { get; } = cache;

    private IOAuthConnectionRepository<TConnection> Repository { get; } = repository;

    private IOAuthApiClientBase OAuthClient { get; } = oauthClient;

    private IOAuthSecretProtectorBase SecretProtector { get; } = secretProtector;

    protected abstract string CacheKeyPrefix { get; }

    public async Task<TContainer?> CreateAsync(string userId, CancellationToken cancellationToken)
    {
        var cached = await Cache.TryGetAsync<TContainer>(CacheKey(userId), token: cancellationToken);
        if (cached.HasValue)
        {
            SetCurrent(cached.Value);
            return cached.Value;
        }

        var connection = await Repository.GetByUserId(userId);
        if (connection is not { Enabled: true })
        {
            return null;
        }

        var refreshToken = SecretProtector.Unprotect(connection.EncryptedRefreshToken);
        var token = await OAuthClient.RefreshAsync(refreshToken, cancellationToken);

        if (!string.IsNullOrEmpty(token.RefreshToken) && token.RefreshToken != refreshToken)
        {
            var rotatedRefreshToken = SecretProtector.Protect(token.RefreshToken);
            await Repository.SetRefreshTokenAsync(userId, rotatedRefreshToken);
        }

        var resolved = BuildContainer(userId, token.AccessToken, connection);

        var lifetime = TimeSpan.FromSeconds(token.ExpiresInSeconds) - ExpiryBuffer;
        if (lifetime > TimeSpan.Zero)
        {
            await Cache.SetAsync(CacheKey(userId), resolved, lifetime, token: cancellationToken);
        }

        SetCurrent(resolved);
        return resolved;
    }

    public void Evict(string userId) => Cache.Remove(CacheKey(userId));

    protected abstract TContainer BuildContainer(string userId, string accessToken, TConnection connection);

    protected abstract void SetCurrent(TContainer container);

    private string CacheKey(string userId) => $"{CacheKeyPrefix}:{userId}";
}
