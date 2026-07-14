using Samhammer.Options.Abstractions;

namespace Vuetrack.Backends.Timetracking;

[Option]
public class TimetrackingOptions
{
    public string ApiBaseUrl { get; init; } = string.Empty;

    public string AuthorizeEndpoint { get; init; } = "https://auth-test.cloud.samhammer.de/auth/realms/timetracking-dev/protocol/openid-connect/auth";

    public string TokenEndpoint { get; init; } = "https://auth-test.cloud.samhammer.de/auth/realms/timetracking-dev/protocol/openid-connect/token";

    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    public string Scopes { get; init; } = "openid profile offline_access";
}
