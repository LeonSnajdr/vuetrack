using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Connections;
using Vuetrack.Api.Features.Integrations.Jira.OAuth;
using ZiggyCreatures.Caching.Fusion;

namespace Vuetrack.Api.Features.Integrations.Jira.Connection;

[Inject]
public class JiraConnectionContextFactory(IConnectionRepository repository, IJiraOAuthApiClient oauthClient, IConnectionSecretProtector secretProtector, IFusionCache cache)
    : ConnectionContextFactory<JiraConnectionContext>(cache, repository, oauthClient, secretProtector), IJiraConnectionContextFactory
{
    protected override IntegrationKey Key => IntegrationKey.Jira;

    protected override JiraConnectionContext BuildContext(string userId, string accessToken, ConnectionModel connection) => new()
    {
        UserId = userId,
        AccessToken = accessToken,
        CloudId = connection.Attributes.GetValueOrDefault(JiraConnectionAttributes.CloudId, string.Empty),
        SiteUrl = connection.Attributes.GetValueOrDefault(JiraConnectionAttributes.SiteUrl, string.Empty),
    };
}

public interface IJiraConnectionContextFactory : IConnectionContextFactory<JiraConnectionContext>;
