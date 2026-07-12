namespace Vuetrack.Connectors.Jira.Activity;

public sealed record JiraWorklogContainer
{
    public required string IssueKey { get; init; }

    public required string IssueSummary { get; init; }

    public required string WorklogId { get; init; }

    public required DateTime Started { get; init; }

    public required long TimeSpentSeconds { get; init; }

    public string? Comment { get; init; }

    public string? Project { get; init; }

    public string? IssueType { get; init; }

    public string? Status { get; init; }
}
