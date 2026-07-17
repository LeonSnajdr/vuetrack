namespace Vuetrack.Backends.Timetracking.Connection;

// Request-scoped, decrypted view of the connection published by the context factory.
public sealed record TimetrackingConnectionContainer
{
    public required string UserId { get; init; }

    public required string AccessToken { get; init; }

    public string? ExternalUserId { get; init; }
}
