namespace Vuetrack.Connectors.Abstractions;

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
