using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Backends.Timetracking.OAuth;
using Vuetrack.OAuth;
using ZiggyCreatures.Caching.Fusion;

namespace Vuetrack.Backends.Timetracking.Connection;

[Inject]
public class TimetrackingConnectionContextFactory(
    ITimetrackingConnectionRepository repository,
    ITimetrackingOAuthApiClient oauthClient,
    ISecretProtector secretProtector,
    ITimetrackingConnectionAccessor accessor,
    IFusionCache cache)
    : OAuthConnectionContextFactory<TimetrackingConnectionContainer, TimetrackingConnectionModel>(cache), ITimetrackingConnectionContextFactory
{
    private ITimetrackingConnectionRepository Repository { get; } = repository;

    private ITimetrackingOAuthApiClient OAuthClient { get; } = oauthClient;

    private ISecretProtector SecretProtector { get; } = secretProtector;

    private ITimetrackingConnectionAccessor Accessor { get; } = accessor;

    protected override string CacheKeyPrefix => "tt-at";

    protected override Task<TimetrackingConnectionModel?> LoadAsync(string userId, CancellationToken cancellationToken) => Repository.GetByUserId(userId);

    protected override Task<OAuthTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken) => OAuthClient.RefreshAsync(refreshToken, cancellationToken);

    protected override Task PersistRotatedTokenAsync(string userId, string encryptedRefreshToken) => Repository.SetRefreshTokenAsync(userId, encryptedRefreshToken);

    protected override string Protect(string plaintext) => SecretProtector.Protect(plaintext);

    protected override string Unprotect(string ciphertext) => SecretProtector.Unprotect(ciphertext);

    protected override TimetrackingConnectionContainer BuildContainer(string userId, string accessToken, TimetrackingConnectionModel connection) => new()
    {
        UserId = userId,
        AccessToken = accessToken,
        ExternalUserId = connection.ExternalUserId,
    };

    protected override void SetCurrent(TimetrackingConnectionContainer container) => Accessor.Current = container;
}

public interface ITimetrackingConnectionContextFactory
{
    Task<TimetrackingConnectionContainer?> CreateAsync(string userId, CancellationToken cancellationToken);

    void Evict(string userId);
}
