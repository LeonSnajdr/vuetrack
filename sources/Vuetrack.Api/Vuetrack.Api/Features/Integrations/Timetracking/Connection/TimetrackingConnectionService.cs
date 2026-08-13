using System.Globalization;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Connections;
using Vuetrack.Api.Features.Integrations.Timetracking.Api;
using Vuetrack.Api.Features.Integrations.Timetracking.OAuth;

namespace Vuetrack.Api.Features.Integrations.Timetracking.Connection;

[InjectAs(typeof(IIntegrationConnectionService))]
public class TimetrackingConnectionService(
    ITimetrackingOAuthApiClient oauthClient,
    ITimetrackingApiClient apiClient,
    IConnectionRepository repository,
    ITimetrackingConnectionContextFactory contextFactory,
    IConnectionSecretProtector secretProtector,
    IIntegrationRegistry registry,
    ILogger<TimetrackingConnectionService> logger)
    : IntegrationConnectionServiceBase(oauthClient, repository, contextFactory, secretProtector, registry, logger)
{
    private ITimetrackingApiClient ApiClient { get; } = apiClient;

    public override IntegrationKey Key => IntegrationKey.Timetracking;

    protected override async Task<ErrorOr<Dictionary<string, string>>> EstablishAsync(OAuthTokenResponse token, CancellationToken cancellationToken)
    {
        var profile = await ApiClient.GetProfileAsync(token.AccessToken, cancellationToken);
        var externalUserId = profile.Id.ToString(CultureInfo.InvariantCulture);

        return new Dictionary<string, string>
        {
            [TimetrackingConnectionAttributes.ExternalUserId] = externalUserId,
        };
    }
}
