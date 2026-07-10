using Microsoft.Extensions.Caching.Memory;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Connectors.Jira.ApiClients;
using Vuetrack.Connectors.Jira.Repositories;

namespace Vuetrack.Connectors.Jira.Services;

[Inject]
public class JiraConnectionContextFactory(IJiraConnectionRepository repository, IJiraOAuthApiClient oauthClient, ISecretProtector secretProtector, IJiraConnectionAccessor accessor, IMemoryCache cache) : IJiraConnectionContextFactory
{
    // Refresh a little early so a token never expires mid-request.
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromSeconds(60);

    private IJiraConnectionRepository Repository { get; } = repository;

    private IJiraOAuthApiClient OAuthClient { get; } = oauthClient;

    private ISecretProtector SecretProtector { get; } = secretProtector;

    private IJiraConnectionAccessor Accessor { get; } = accessor;

    private IMemoryCache Cache { get; } = cache;

    public async Task<JiraConnectionContainer?> CreateAsync(string userId, CancellationToken cancellationToken)
    {
        if (Cache.TryGetValue(CacheKey(userId), out JiraConnectionContainer? cached) && cached is not null)
        {
            Accessor.Current = cached;
            return cached;
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
            connection.EncryptedRefreshToken = SecretProtector.Protect(token.RefreshToken);
            await Repository.Save(connection);
        }

        var resolved = new JiraConnectionContainer
        {
            UserId = userId,
            AccessToken = token.AccessToken,
            CloudId = connection.CloudId,
            SiteUrl = connection.SiteUrl,
        };

        var lifetime = TimeSpan.FromSeconds(token.ExpiresInSeconds) - ExpiryBuffer;
        if (lifetime > TimeSpan.Zero)
        {
            Cache.Set(CacheKey(userId), resolved, lifetime);
        }

        Accessor.Current = resolved;
        return resolved;
    }

    public void Evict(string userId) => Cache.Remove(CacheKey(userId));

    private static string CacheKey(string userId) => $"jira-at:{userId}";
}

public interface IJiraConnectionContextFactory
{
    /// <summary>
    /// Resolves the current user's Jira connection (from cache, else refresh + persist rotation) and
    /// publishes it on the scoped <see cref="IJiraConnectionAccessor"/> so <see cref="ApiClients.JiraApiClient"/>
    /// picks it up. Returns the same connection (or <c>null</c> when the user has no enabled connection).
    /// </summary>
    Task<JiraConnectionContainer?> CreateAsync(string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Drops the cached access token for a user so the next <see cref="CreateAsync"/> forces a refresh.
    /// Call this when Jira rejects the token (401/403) so a revoked/rotated token is not reused.
    /// </summary>
    void Evict(string userId);
}
