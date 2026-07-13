using Samhammer.Options.Abstractions;

namespace Vuetrack.Backends.Timetracking;

[Option]
public class TimetrackingOptions
{
    public string IdentityBaseUrl { get; init; } = "https://auth-test.cloud.samhammer.de/auth/realms/timetracking-dev";

    public string ApiBaseUrl { get; init; } = string.Empty;

    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    public string Scopes { get; init; } = "openid profile offline_access";
}
