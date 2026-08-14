using Samhammer.Options.Abstractions;
using Vuetrack.Api.Features.Integrations.Connections;

namespace Vuetrack.Api.Features.Integrations.Timetracking;

[Option]
public class TimetrackingOptions : OAuthOptions
{
    public required string ApiBaseUrl { get; init; }
}
