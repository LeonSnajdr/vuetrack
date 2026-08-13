using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Connections;
using Vuetrack.Api.Features.Integrations.Timetracking.OAuth;
using ZiggyCreatures.Caching.Fusion;

namespace Vuetrack.Api.Features.Integrations.Timetracking.Connection;

[Inject]
public class TimetrackingConnectionContextFactory(IConnectionRepository repository, ITimetrackingOAuthApiClient oauthClient, IConnectionSecretProtector secretProtector, IFusionCache cache)
    : ConnectionContextFactory<TimetrackingConnectionContext>(cache, repository, oauthClient, secretProtector), ITimetrackingConnectionContextFactory
{
    protected override IntegrationKey Key => IntegrationKey.Timetracking;

    protected override TimetrackingConnectionContext BuildContext(string userId, string accessToken, ConnectionModel connection) => new()
    {
        UserId = userId,
        AccessToken = accessToken,
        ExternalUserId = connection.Attributes.GetValueOrDefault(TimetrackingConnectionAttributes.ExternalUserId),
    };
}

public interface ITimetrackingConnectionContextFactory : IConnectionContextFactory<TimetrackingConnectionContext>;
