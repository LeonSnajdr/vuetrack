using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Backends.Timetracking.OAuth;
using ZiggyCreatures.Caching.Fusion;

namespace Vuetrack.Backends.Timetracking.Connection;

[Inject]
public class TimetrackingConnectionContextFactory(ITimetrackingConnectionRepository repository, ITimetrackingOAuthApiClient oauthClient, ISecretProtector secretProtector, ITimetrackingConnectionAccessor accessor, IFusionCache cache) : ITimetrackingConnectionContextFactory
{
    // Refresh a little early so a token never expires mid-request.
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromSeconds(60);

    private ITimetrackingConnectionRepository Repository { get; } = repository;

    private ITimetrackingOAuthApiClient OAuthClient { get; } = oauthClient;

    private ISecretProtector SecretProtector { get; } = secretProtector;

    private ITimetrackingConnectionAccessor Accessor { get; } = accessor;

    private IFusionCache Cache { get; } = cache;

    public async Task<TimetrackingConnectionContainer?> CreateAsync(string userId, CancellationToken cancellationToken)
    {
        var cached = await Cache.TryGetAsync<TimetrackingConnectionContainer>(CacheKey(userId), token: cancellationToken);
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

        var resolved = new TimetrackingConnectionContainer
        {
            UserId = userId,
            AccessToken = token.AccessToken,
            ExternalUserId = connection.ExternalUserId,
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

    private static string CacheKey(string userId) => $"tt-at:{userId}";
}

public interface ITimetrackingConnectionContextFactory
{
    Task<TimetrackingConnectionContainer?> CreateAsync(string userId, CancellationToken cancellationToken);

    void Evict(string userId);
}
