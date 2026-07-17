using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Backends.Timetracking.OAuth;
using Vuetrack.OAuth;
using ZiggyCreatures.Caching.Fusion;

namespace Vuetrack.Backends.Timetracking.Connection;

[Inject]
public class TimetrackingConnectionContextFactory(ITimetrackingConnectionRepository repository, ITimetrackingOAuthApiClient oauthClient, ITimetrackingSecretProtector secretProtector, ITimetrackingConnectionAccessor accessor, IFusionCache cache) : OAuthConnectionContextFactory<TimetrackingConnectionContainer, TimetrackingConnectionModel>(cache, repository, oauthClient, secretProtector), ITimetrackingConnectionContextFactory
{
    private ITimetrackingConnectionAccessor Accessor { get; } = accessor;

    protected override string CacheKeyPrefix => "tt-at";

    protected override TimetrackingConnectionContainer BuildContainer(string userId, string accessToken, TimetrackingConnectionModel connection) => new()
    {
        UserId = userId,
        AccessToken = accessToken,
        ExternalUserId = connection.ExternalUserId,
    };

    protected override void SetCurrent(TimetrackingConnectionContainer container) => Accessor.Current = container;
}

public interface ITimetrackingConnectionContextFactory : IOAuthConnectionContextFactory<TimetrackingConnectionContainer>;
