namespace Vuetrack.Connectors.Abstractions.Metadata;

public enum ActivityKind
{
    Unknown,
    Worklog,
    Call,
    Commit,
    IssueCreated,
    StatusTransition,
    Comment,
    ContentEdit,
    Planning,
    Admin,
}
