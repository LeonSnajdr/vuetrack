using Microsoft.Extensions.Caching.Memory;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Connectors.Jira.ApiClients;
using Vuetrack.Connectors.Jira.Repositories;

namespace Vuetrack.Connectors.Jira.Services;

[Inject]
public class JiraConnectionContextFactory(
    IJiraConnectionRepository repository,
    IJiraOAuthApiClient oauthClient,
    ISecretProtector secretProtector,
    IMemoryCache cache) : IJiraConnectionContextFactory
{
    // Refresh a little early so a token never expires mid-request.
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromSeconds(60);

    private IJiraConnectionRepository Repository { get; } = repository;

    private IJiraOAuthApiClient OAuthClient { get; } = oauthClient;

    private ISecretProtector SecretProtector { get; } = secretProtector;

    private IMemoryCache Cache { get; } = cache;

    public async Task<JiraConnection?> CreateAsync(string userId, CancellationToken cancellationToken)
    {
        if (Cache.TryGetValue(CacheKey(userId), out JiraConnection? cached) && cached is not null)
        {
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

        var resolved = new JiraConnection
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

        return resolved;
    }

    public void Evict(string userId) => Cache.Remove(CacheKey(userId));

    private static string CacheKey(string userId) => $"jira-at:{userId}";
}

public interface IJiraConnectionContextFactory
{
    /// <summary>
    /// Resolves the current user's Jira connection (from cache, else refresh + persist rotation).
    /// The caller must assign the result to <see cref="IJiraConnectionAccessor.Current"/> in its own
    /// call frame so <c>JiraAuthHandler</c> / <see cref="ApiClients.JiraApiClient"/> pick it up — an
    /// <see cref="AsyncLocal{T}"/> set inside this awaited method would not flow back to the caller.
    /// </summary>
    Task<JiraConnection?> CreateAsync(string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Drops the cached access token for a user so the next <see cref="CreateAsync"/> forces a refresh.
    /// Call this when Jira rejects the token (401/403) so a revoked/rotated token is not reused.
    /// </summary>
    void Evict(string userId);
}
