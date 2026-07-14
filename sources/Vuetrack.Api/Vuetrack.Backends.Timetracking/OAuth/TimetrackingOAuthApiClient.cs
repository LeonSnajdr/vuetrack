using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.Backends.Abstractions;
using Vuetrack.OAuth;

namespace Vuetrack.Backends.Timetracking.OAuth;

[Inject]
public class TimetrackingOAuthApiClient(HttpClient httpClient, IOptions<TimetrackingOptions> options, ILogger<TimetrackingOAuthApiClient> logger) : OAuthApiClientBase(httpClient, logger, options), ITimetrackingOAuthApiClient
{
    protected override string ProviderName => nameof(BackendKey.Timetracking);
}

public interface ITimetrackingOAuthApiClient : IOAuthApiClientBase;
