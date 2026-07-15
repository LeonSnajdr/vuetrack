using Vuetrack.Connectors.Abstractions.Metadata;

namespace Vuetrack.Connectors.Jira.Activity;

// Connector-owned metadata keys. These live under the jira.* namespace and are only understood by the
// Jira connector and by explainability/UI code that opts into reading them; the engine works off the
// shared MetadataKeys instead.
public static class JiraMetadataKeys
{
    public static readonly MetadataKey<string> IssueKey = new("jira.issue.key");

    public static readonly MetadataKey<string> IssueId = new("jira.issue.id");

    public static readonly MetadataKey<string> ProjectKey = new("jira.project.key");

    public static readonly MetadataKey<string> ProjectName = new("jira.project.name");

    public static readonly MetadataKey<string> IssueType = new("jira.issue.type");

    public static readonly MetadataKey<string> Status = new("jira.status");

    public static readonly MetadataKey<string> ParentKey = new("jira.parent.key");

    public static readonly MetadataKey<IReadOnlyList<string>> Labels = new("jira.labels");

    public static readonly MetadataKey<IReadOnlyList<string>> Components = new("jira.components");

    public static readonly MetadataKey<string> WorklogId = new("jira.worklog.id");

    public static readonly MetadataKey<string> CommentId = new("jira.comment.id");

    public static readonly MetadataKey<string> ChangelogId = new("jira.changelog.id");

    public static readonly MetadataKey<string> FieldId = new("jira.field.id");

    public static readonly MetadataKey<string> FieldName = new("jira.field.name");

    public static readonly MetadataKey<JiraFieldTransition> Transition = new("jira.transition");
}

// A structural before/after change captured from a changelog item, compared as data rather than an
// encoded string.
public sealed record JiraFieldTransition(string? FromId, string? FromValue, string? ToId, string? ToValue);
