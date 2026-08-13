using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Connections;
using Vuetrack.Api.Features.Integrations.Github.OAuth;
using ZiggyCreatures.Caching.Fusion;

namespace Vuetrack.Api.Features.Integrations.Github.Connection;

[Inject]
public class GithubConnectionContextFactory(IConnectionRepository repository, IGithubOAuthApiClient oauthClient, IConnectionSecretProtector secretProtector, IFusionCache cache)
    : ConnectionContextFactory<GithubConnectionContext>(cache, repository, oauthClient, secretProtector), IGithubConnectionContextFactory
{
    protected override IntegrationKey Key => IntegrationKey.Github;

    protected override GithubConnectionContext BuildContext(string userId, string accessToken, ConnectionModel connection) => new()
    {
        UserId = userId,
        AccessToken = accessToken,
        Login = connection.Attributes.GetValueOrDefault(GithubConnectionAttributes.Login, string.Empty),
    };
}

public interface IGithubConnectionContextFactory : IConnectionContextFactory<GithubConnectionContext>;
