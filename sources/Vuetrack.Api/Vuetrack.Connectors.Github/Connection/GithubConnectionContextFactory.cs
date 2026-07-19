using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Connectors.Github.OAuth;
using Vuetrack.OAuth;
using ZiggyCreatures.Caching.Fusion;

namespace Vuetrack.Connectors.Github.Connection;

[Inject]
public class GithubConnectionContextFactory(IGithubConnectionRepository repository, IGithubOAuthApiClient oauthClient, IGithubConnectorSecretProtector githubConnectorSecretProtector, IGithubConnectionAccessor accessor, IFusionCache cache) : OAuthConnectionContextFactory<GithubConnectionContainer, GithubConnectionModel>(cache, repository, oauthClient, githubConnectorSecretProtector), IGithubConnectionContextFactory
{
    private IGithubConnectionAccessor Accessor { get; } = accessor;

    protected override string CacheKeyPrefix => "github-at";

    protected override GithubConnectionContainer BuildContainer(string userId, string accessToken, GithubConnectionModel connection) => new()
    {
        UserId = userId,
        AccessToken = accessToken,
        Login = connection.Login,
    };

    protected override void SetCurrent(GithubConnectionContainer container) => Accessor.Current = container;
}

public interface IGithubConnectionContextFactory : IOAuthConnectionContextFactory<GithubConnectionContainer>;
