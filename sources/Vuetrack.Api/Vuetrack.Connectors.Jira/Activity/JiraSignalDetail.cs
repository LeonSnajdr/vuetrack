using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Connectors.Jira.Activity;

// Connector-specific facts behind a Jira ActivitySignal. Shared issue context plus the per-event fields;
// members irrelevant to a given event kind are simply left null/empty. Opaque to the engine; the model
// reasons over its serialized shape and connector-aware code downcasts to it.
public sealed record JiraSignalDetail : IConnectorSignalDetail
{
    public required string IssueKey { get; init; }

    public string? IssueId { get; init; }

    public string? Summary { get; init; }

    public string? ProjectKey { get; init; }

    public string? ProjectName { get; init; }

    public string? IssueType { get; init; }

    public string? Status { get; init; }

    public string? ParentKey { get; init; }

    public IReadOnlyList<string> Labels { get; init; } = [];

    public IReadOnlyList<string> Components { get; init; } = [];

    public string? ActorId { get; init; }

    public string? SourceUrl { get; init; }

    public string? CommentText { get; init; }

    public string? WorklogId { get; init; }

    public string? CommentId { get; init; }

    public string? ChangelogId { get; init; }

    public string? FieldId { get; init; }

    public string? FieldName { get; init; }

    public JiraFieldTransition? Transition { get; init; }
}

// A structural before/after change captured from a changelog item, compared as data rather than an
// encoded string.
public sealed record JiraFieldTransition(string? FromId, string? FromValue, string? ToId, string? ToValue);
