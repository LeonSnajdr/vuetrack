using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Abstractions.Metadata;
using Vuetrack.Connectors.Jira.Activity.Dtos;
using Vuetrack.Connectors.Jira.Internal;

namespace Vuetrack.Connectors.Jira.Activity;

// Maps already-filtered, typed Jira DTOs (plus their issue context) to ActivitySignals carrying typed
// metadata. No JSON traversal or string date parsing happens here — that is the DTO layer's job.
public static class JiraActivityMapper
{
    public static ActivitySignal ToWorklogSignal(JiraIssueContext context, JiraWorklogDto worklog, string siteUrl)
    {
        var started = worklog.Started!.Value.UtcDateTime;
        var ended = started.AddSeconds(worklog.TimeSpentSeconds);
        var commentText = AdfTextExtractor.Extract(worklog.Comment);

        var builder = new SignalMetadataBuilder();
        ApplyIssueContext(builder, context, siteUrl);
        builder.Set(MetadataKeys.ActivityKind, ActivityKind.Worklog);
        builder.SetIfNotNull(MetadataKeys.ActorId, worklog.Author?.AccountId);
        builder.SetIfNotNull(MetadataKeys.DisplayComment, commentText);
        builder.SetIfNotNull(JiraMetadataKeys.WorklogId, worklog.Id);
        var metadata = builder.Build();

        var externalId = $"{context.Key}:worklog:{worklog.Id}";

        return new ActivitySignal
        {
            ConnectorKey = ConnectorKey.Jira,
            ExternalId = externalId,
            DateStarted = started,
            DateEnded = ended,
            Metadata = metadata,
        };
    }

    public static ActivitySignal ToCommentSignal(JiraIssueContext context, JiraCommentDto comment, string siteUrl)
    {
        var created = comment.Created!.Value.UtcDateTime;
        var text = AdfTextExtractor.Extract(comment.Body);

        var builder = new SignalMetadataBuilder();
        ApplyIssueContext(builder, context, siteUrl);
        builder.Set(MetadataKeys.ActivityKind, ActivityKind.Comment);
        builder.SetIfNotNull(MetadataKeys.ActorId, comment.Author?.AccountId);
        builder.SetIfNotNull(MetadataKeys.DisplayComment, text);
        builder.SetIfNotNull(JiraMetadataKeys.CommentId, comment.Id);
        var metadata = builder.Build();

        var externalId = $"{context.Key}:comment:{comment.Id}";

        return new ActivitySignal
        {
            ConnectorKey = ConnectorKey.Jira,
            ExternalId = externalId,
            DateStarted = created,
            DateEnded = null,
            Metadata = metadata,
        };
    }

    public static ActivitySignal ToChangeSignal(JiraIssueContext context, JiraChangelogDto changelog, JiraChangelogItemDto item, int itemIndex, string siteUrl)
    {
        var created = changelog.Created!.Value.UtcDateTime;
        var kind = Classify(item);
        var transition = new JiraFieldTransition(item.From, item.FromString, item.To, item.ToDisplay);

        var builder = new SignalMetadataBuilder();
        ApplyIssueContext(builder, context, siteUrl);
        builder.Set(MetadataKeys.ActivityKind, kind);
        builder.SetIfNotNull(MetadataKeys.ActorId, changelog.Author?.AccountId);
        builder.SetIfNotNull(JiraMetadataKeys.ChangelogId, changelog.Id);
        builder.SetIfNotNull(JiraMetadataKeys.FieldId, item.FieldId);
        builder.SetIfNotNull(JiraMetadataKeys.FieldName, item.Field);
        builder.Set(JiraMetadataKeys.Transition, transition);
        var metadata = builder.Build();

        var externalId = $"{context.Key}:changelog:{changelog.Id}:{itemIndex}";

        return new ActivitySignal
        {
            ConnectorKey = ConnectorKey.Jira,
            ExternalId = externalId,
            DateStarted = created,
            DateEnded = null,
            Metadata = metadata,
        };
    }

    // Classifies a changelog item into an activity kind the engine's evidence rules understand.
    public static ActivityKind Classify(JiraChangelogItemDto item)
    {
        var field = item.FieldId ?? item.Field;
        var normalized = field?.ToLowerInvariant();

        return normalized switch
        {
            "status" => ActivityKind.StatusTransition,
            "summary" or "description" => ActivityKind.ContentEdit,
            "priority" or "assignee" or "resolution" or "reporter" or "labels" or "parent" => ActivityKind.Planning,
            "project" or "key" => ActivityKind.Admin,
            _ => ActivityKind.Planning,
        };
    }

    private static void ApplyIssueContext(SignalMetadataBuilder builder, JiraIssueContext context, string siteUrl)
    {
        var title = $"{context.Key} {context.Summary}".TrimEnd();
        var url = BrowseUrl(siteUrl, context.Key);
        var correlation = new List<string> { context.Key };

        builder.Set(MetadataKeys.SubjectWorkItemId, context.Key);
        builder.Set(MetadataKeys.DisplayTitle, title);
        builder.Set(MetadataKeys.SourceUrl, url);
        builder.Set(MetadataKeys.CorrelationKeys, correlation);
        builder.SetIfNotNull(MetadataKeys.DisplayProject, context.ProjectName);

        builder.Set(JiraMetadataKeys.IssueKey, context.Key);
        builder.SetIfNotNull(JiraMetadataKeys.IssueId, context.Id);
        builder.SetIfNotNull(JiraMetadataKeys.ProjectKey, context.ProjectKey);
        builder.SetIfNotNull(JiraMetadataKeys.ProjectName, context.ProjectName);
        builder.SetIfNotNull(JiraMetadataKeys.IssueType, context.IssueType);
        builder.SetIfNotNull(JiraMetadataKeys.Status, context.Status);
        builder.SetIfNotNull(JiraMetadataKeys.ParentKey, context.ParentKey);

        if (context.Labels.Count > 0)
        {
            builder.Set(JiraMetadataKeys.Labels, context.Labels);
        }

        if (context.Components.Count > 0)
        {
            builder.Set(JiraMetadataKeys.Components, context.Components);
        }
    }

    private static string BrowseUrl(string siteUrl, string issueKey)
    {
        var trimmed = siteUrl.TrimEnd('/');
        return $"{trimmed}/browse/{issueKey}";
    }
}
