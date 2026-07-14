using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Connectors.Jira.OAuth;
using Vuetrack.OAuth;
using ZiggyCreatures.Caching.Fusion;

namespace Vuetrack.Connectors.Jira.Connection;

[Inject]
public class JiraConnectionContextFactory(
    IJiraConnectionRepository repository,
    IJiraOAuthApiClient oauthClient,
    ISecretProtector secretProtector,
    IJiraConnectionAccessor accessor,
    IFusionCache cache)
    : OAuthConnectionContextFactory<JiraConnectionContainer, JiraConnectionModel>(cache), IJiraConnectionContextFactory
{
    private IJiraConnectionRepository Repository { get; } = repository;

    private IJiraOAuthApiClient OAuthClient { get; } = oauthClient;

    private ISecretProtector SecretProtector { get; } = secretProtector;

    private IJiraConnectionAccessor Accessor { get; } = accessor;

    protected override string CacheKeyPrefix => "jira-at";

    protected override Task<JiraConnectionModel?> LoadAsync(string userId, CancellationToken cancellationToken) => Repository.GetByUserId(userId);

    protected override Task<OAuthTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken) => OAuthClient.RefreshAsync(refreshToken, cancellationToken);

    protected override Task PersistRotatedTokenAsync(string userId, string encryptedRefreshToken) => Repository.SetRefreshTokenAsync(userId, encryptedRefreshToken);

    protected override string Protect(string plaintext) => SecretProtector.Protect(plaintext);

    protected override string Unprotect(string ciphertext) => SecretProtector.Unprotect(ciphertext);

    protected override JiraConnectionContainer BuildContainer(string userId, string accessToken, JiraConnectionModel connection) => new()
    {
        UserId = userId,
        AccessToken = accessToken,
        CloudId = connection.CloudId,
        SiteUrl = connection.SiteUrl,
    };

    protected override void SetCurrent(JiraConnectionContainer container) => Accessor.Current = container;
}

public interface IJiraConnectionContextFactory
{
    Task<JiraConnectionContainer?> CreateAsync(string userId, CancellationToken cancellationToken);

    void Evict(string userId);
}
