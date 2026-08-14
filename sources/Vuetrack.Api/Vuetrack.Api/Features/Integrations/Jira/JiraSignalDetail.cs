using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Activity;

namespace Vuetrack.Api.Features.Integrations.Jira;

public sealed record JiraSignalDetail : IActivitySignalDetail
{
    public required string IssueKey { get; init; }

    public string? Title { get; init; }

    public string? IssueType { get; init; }

    public string? Status { get; init; }

    public string? ParentKey { get; init; }

    public string? ParentTitle { get; init; }

    public JiraFieldTransition? Transition { get; init; }
}

public sealed record JiraFieldTransition(string? FromValue, string? ToValue);
