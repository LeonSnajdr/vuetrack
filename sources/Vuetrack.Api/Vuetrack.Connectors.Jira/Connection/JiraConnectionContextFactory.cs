using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Connectors.Jira.OAuth;
using ZiggyCreatures.Caching.Fusion;

namespace Vuetrack.Connectors.Jira.Connection;

[Inject]
public class JiraConnectionContextFactory(IJiraConnectionRepository repository, IJiraOAuthApiClient oauthClient, ISecretProtector secretProtector, IJiraConnectionAccessor accessor, IFusionCache cache) : IJiraConnectionContextFactory
{
    // Refresh a little early so a token never expires mid-request.
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromSeconds(60);

    private IJiraConnectionRepository Repository { get; } = repository;

    private IJiraOAuthApiClient OAuthClient { get; } = oauthClient;

    private ISecretProtector SecretProtector { get; } = secretProtector;

    private IJiraConnectionAccessor Accessor { get; } = accessor;

    private IFusionCache Cache { get; } = cache;

    public async Task<JiraConnectionContainer?> CreateAsync(string userId, CancellationToken cancellationToken)
    {
        var cached = await Cache.TryGetAsync<JiraConnectionContainer>(CacheKey(userId), token: cancellationToken);
        if (cached.HasValue)
        {
            Accessor.Current = cached.Value;
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
            await Cache.SetAsync(CacheKey(userId), resolved, lifetime, token: cancellationToken);
        }

        Accessor.Current = resolved;
        return resolved;
    }

    public void Evict(string userId) => Cache.Remove(CacheKey(userId));

    private static string CacheKey(string userId) => $"jira-at:{userId}";
}

public interface IJiraConnectionContextFactory
{
    Task<JiraConnectionContainer?> CreateAsync(string userId, CancellationToken cancellationToken);

    void Evict(string userId);
}
