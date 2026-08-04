namespace Vuetrack.Backends.Timetracking.Connection;

public sealed record TimetrackingConnectionContainer
{
    public required string UserId { get; init; }

    public required string AccessToken { get; init; }

    public string? ExternalUserId { get; init; }
}
