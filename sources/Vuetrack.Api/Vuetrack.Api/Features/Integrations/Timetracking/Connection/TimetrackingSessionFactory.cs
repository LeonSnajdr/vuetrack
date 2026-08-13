using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Connections;
using Vuetrack.Api.Features.Integrations.Timetracking.Api;
using Vuetrack.Api.Features.Integrations.Timetracking.OAuth;
using ZiggyCreatures.Caching.Fusion;

namespace Vuetrack.Api.Features.Integrations.Timetracking.Connection;

[Inject]
public class TimetrackingSessionFactory(
    HttpClient httpClient,
    IConnectionRepository repository,
    ITimetrackingOAuthApiClient oauthClient,
    IConnectionSecretProtector secretProtector,
    IFusionCache cache)
    : ConnectionSessionFactory<ITimetrackingSession>(cache, repository, oauthClient, secretProtector), ITimetrackingSessionFactory
{
    private HttpClient HttpClient { get; } = httpClient;

    protected override IntegrationKey Key => IntegrationKey.Timetracking;

    protected override ITimetrackingSession CreateSession(ConnectionCredentials credentials)
    {
        var externalUserId = credentials.Attributes.GetValueOrDefault(TimetrackingConnectionAttributes.ExternalUserId);

        return new TimetrackingSession(HttpClient, credentials.AccessToken, externalUserId);
    }
}

public interface ITimetrackingSessionFactory : IConnectionSessionFactory<ITimetrackingSession>;
