namespace Vuetrack.Backends.Timetracking.Connection;

/// <summary>
/// The resolved timetracking connection for the current operation: the live access token plus the
/// external user id needed for write operations. Held ambiently via
/// <see cref="ITimetrackingConnectionAccessor"/> rather than threaded through method calls.
/// </summary>
public sealed record TimetrackingConnectionContainer
{
    public required string UserId { get; init; }

    public required string AccessToken { get; init; }

    public string? ExternalUserId { get; init; }
}
