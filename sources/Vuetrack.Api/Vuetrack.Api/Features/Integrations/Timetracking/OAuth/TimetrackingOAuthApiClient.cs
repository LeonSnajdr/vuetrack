using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Connections;

namespace Vuetrack.Api.Features.Integrations.Timetracking.OAuth;

[Inject]
public class TimetrackingOAuthApiClient(HttpClient httpClient, IOptions<TimetrackingOptions> options, ILogger<TimetrackingOAuthApiClient> logger) : OAuthApiClientBase(httpClient, logger, options), ITimetrackingOAuthApiClient
{
    protected override IntegrationKey Key => IntegrationKey.Timetracking;
}

public interface ITimetrackingOAuthApiClient : IOAuthApiClientBase;
