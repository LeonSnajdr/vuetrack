namespace Vuetrack.Connectors.Jira.Connection;

// Request-scoped, decrypted view of the connection published by the context factory.
public sealed record JiraConnectionContainer
{
    public required string UserId { get; init; }

    public required string AccessToken { get; init; }

    public required string CloudId { get; init; }

    public required string SiteUrl { get; init; }
}
