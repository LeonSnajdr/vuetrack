using Vuetrack.Api.Features.Integrations.Abstractions;
using ZiggyCreatures.Caching.Fusion;

namespace Vuetrack.Api.Features.Integrations.Connections;

public abstract class ConnectionSessionFactory<TSession>(
    IFusionCache cache,
    IConnectionRepository repository,
    IOAuthApiClientBase oauthClient,
    IConnectionSecretProtector secretProtector)
    : IConnectionSessionFactory<TSession>
    where TSession : class
{
    // Refresh a little early so a token never expires mid-request.
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromSeconds(60);

    private IFusionCache Cache { get; } = cache;

    private IConnectionRepository Repository { get; } = repository;

    private IOAuthApiClientBase OAuthClient { get; } = oauthClient;

    private IConnectionSecretProtector SecretProtector { get; } = secretProtector;

    protected abstract IntegrationKey Key { get; }

    public async Task<TSession?> OpenAsync(string userId, CancellationToken cancellationToken)
    {
        var credentials = await ResolveCredentialsAsync(userId, cancellationToken);
        if (credentials is null)
        {
            return null;
        }

        return CreateSession(credentials);
    }

    public async Task EvictAsync(string userId, CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey(userId);

        await Cache.RemoveAsync(cacheKey, token: cancellationToken);
    }

    protected abstract TSession CreateSession(ConnectionCredentials credentials);

    private async Task<ConnectionCredentials?> ResolveCredentialsAsync(string userId, CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey(userId);

        var cached = await Cache.TryGetAsync<ConnectionCredentials>(cacheKey, token: cancellationToken);
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

        var credentials = new ConnectionCredentials
        {
            AccessToken = token.AccessToken,
            Attributes = connection.Attributes,
        };

        var lifetime = TimeSpan.FromSeconds(token.ExpiresInSeconds) - ExpiryBuffer;
        if (lifetime > TimeSpan.Zero)
        {
            await Cache.SetAsync(cacheKey, credentials, lifetime, token: cancellationToken);
        }

        return credentials;
    }

    private string BuildCacheKey(string userId) => $"connection:{Key}:{userId}";
}

public interface IConnectionSessionFactory
{
    Task EvictAsync(string userId, CancellationToken cancellationToken);
}

public interface IConnectionSessionFactory<TSession> : IConnectionSessionFactory
{
    Task<TSession?> OpenAsync(string userId, CancellationToken cancellationToken);
}
