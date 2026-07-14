using Samhammer.Options.Abstractions;
using Vuetrack.OAuth;

namespace Vuetrack.Backends.Timetracking;

[Option]
public class TimetrackingOptions : OAuthOptions
{
    public required string ApiBaseUrl { get; init; }
}
