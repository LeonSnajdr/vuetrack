namespace Vuetrack.Connectors.Jira.Services;

/// <summary>
/// The resolved Jira connection for the current operation: the live access token plus the
/// site identifiers needed to address the Jira Cloud API. Held ambiently via
/// <see cref="IJiraConnectionAccessor"/> rather than threaded through method calls.
/// </summary>
public sealed record JiraConnection
{
    public required string UserId { get; init; }

    public required string AccessToken { get; init; }

    public required string CloudId { get; init; }

    public required string SiteUrl { get; init; }
}
