using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Samhammer.DependencyInjection.Attributes;
using Vuetrack.OAuth;

namespace Vuetrack.Backends.Timetracking.OAuth;

[Inject]
public class TimetrackingOAuthApiClient(HttpClient httpClient, IOptions<TimetrackingOptions> options, ILogger<TimetrackingOAuthApiClient> logger)
    : OAuthApiClientBase(httpClient, logger), ITimetrackingOAuthApiClient
{
    private IOptions<TimetrackingOptions> Options { get; } = options;

    protected override string ProviderName => "Timetracking";

    protected override string AuthorizeEndpoint => Options.Value.AuthorizeEndpoint;

    protected override string TokenEndpoint => Options.Value.TokenEndpoint;

    protected override string ClientId => Options.Value.ClientId;

    protected override string ClientSecret => Options.Value.ClientSecret;

    protected override string Scopes => Options.Value.Scopes;
}

public interface ITimetrackingOAuthApiClient : IOAuthApiClientBase;
