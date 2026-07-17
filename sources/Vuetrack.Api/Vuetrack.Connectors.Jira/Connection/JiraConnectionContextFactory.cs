using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Connectors.Jira.OAuth;
using Vuetrack.OAuth;
using ZiggyCreatures.Caching.Fusion;

namespace Vuetrack.Connectors.Jira.Connection;

[Inject]
public class JiraConnectionContextFactory(IJiraConnectionRepository repository, IJiraOAuthApiClient oauthClient, IJiraConnectorSecretProtector jiraConnectorSecretProtector, IJiraConnectionAccessor accessor, IFusionCache cache) : OAuthConnectionContextFactory<JiraConnectionContainer, JiraConnectionModel>(cache, repository, oauthClient, jiraConnectorSecretProtector), IJiraConnectionContextFactory
{
    private IJiraConnectionAccessor Accessor { get; } = accessor;

    protected override string CacheKeyPrefix => "jira-at";

    protected override JiraConnectionContainer BuildContainer(string userId, string accessToken, JiraConnectionModel connection) => new()
    {
        UserId = userId,
        AccessToken = accessToken,
        CloudId = connection.CloudId,
        SiteUrl = connection.SiteUrl,
    };

    protected override void SetCurrent(JiraConnectionContainer container) => Accessor.Current = container;
}

public interface IJiraConnectionContextFactory : IOAuthConnectionContextFactory<JiraConnectionContainer>;
