namespace Vuetrack.Api.Features.Integrations.Activity;

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
