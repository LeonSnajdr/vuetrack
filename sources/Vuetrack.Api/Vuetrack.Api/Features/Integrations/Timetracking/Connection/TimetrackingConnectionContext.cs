namespace Vuetrack.Api.Features.Integrations.Timetracking.Connection;

public sealed record TimetrackingConnectionContext
{
    public required string UserId { get; init; }

    public required string AccessToken { get; init; }

    public string? ExternalUserId { get; init; }
}

public static class TimetrackingConnectionAttributes
{
    public const string ExternalUserId = "externalUserId";
}
