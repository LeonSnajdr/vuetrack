using ZiggyCreatures.Caching.Fusion;

namespace Vuetrack.OAuth;

/// <summary>
/// Shared per-user OAuth2 3LO runtime flow: serve a cached access token, otherwise load the stored
/// connection, refresh the access token, rotate + persist the refresh token when it changes, cache the
/// result until shortly before expiry and publish it to the ambient accessor.
/// Concrete platforms wire their repository, OAuth client, secret protector, accessor and container
/// through the abstract members.
/// </summary>
public abstract class OAuthConnectionContextFactory<TContainer, TConnection>(IFusionCache cache)
    where TContainer : class
    where TConnection : OAuthConnectionModel
{
    // Refresh a little early so a token never expires mid-request.
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromSeconds(60);

    private IFusionCache Cache { get; } = cache;

    protected abstract string CacheKeyPrefix { get; }

    public async Task<TContainer?> CreateAsync(string userId, CancellationToken cancellationToken)
    {
        var cached = await Cache.TryGetAsync<TContainer>(CacheKey(userId), token: cancellationToken);
        if (cached.HasValue)
        {
            SetCurrent(cached.Value);
            return cached.Value;
        }

        var connection = await LoadAsync(userId, cancellationToken);
        if (connection is not { Enabled: true })
        {
            return null;
        }

        var refreshToken = Unprotect(connection.EncryptedRefreshToken);
        var token = await RefreshAsync(refreshToken, cancellationToken);

        if (!string.IsNullOrEmpty(token.RefreshToken) && token.RefreshToken != refreshToken)
        {
            var rotatedRefreshToken = Protect(token.RefreshToken);
            await PersistRotatedTokenAsync(userId, rotatedRefreshToken);
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

    protected abstract Task<TConnection?> LoadAsync(string userId, CancellationToken cancellationToken);

    protected abstract Task<OAuthTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    protected abstract Task PersistRotatedTokenAsync(string userId, string encryptedRefreshToken);

    protected abstract string Protect(string plaintext);

    protected abstract string Unprotect(string ciphertext);

    protected abstract TContainer BuildContainer(string userId, string accessToken, TConnection connection);

    protected abstract void SetCurrent(TContainer container);

    private string CacheKey(string userId) => $"{CacheKeyPrefix}:{userId}";
}
