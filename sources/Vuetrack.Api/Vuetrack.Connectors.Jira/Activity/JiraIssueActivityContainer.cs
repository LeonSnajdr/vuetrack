namespace Vuetrack.Connectors.Jira.Activity;

public sealed record JiraIssueActivityContainer
{
    public required string IssueKey { get; init; }

    public required string Summary { get; init; }

    public required DateTimeOffset Updated { get; init; }

    public string? Project { get; init; }

    public string? IssueType { get; init; }

    public string? Status { get; init; }
}
