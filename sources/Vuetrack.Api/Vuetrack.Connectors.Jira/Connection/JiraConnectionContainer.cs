namespace Vuetrack.Connectors.Jira.Connection;

public sealed record JiraConnectionContainer
{
    public required string UserId { get; init; }

    public required string AccessToken { get; init; }

    public required string CloudId { get; init; }

    public required string SiteUrl { get; init; }
}
