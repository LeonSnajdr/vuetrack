using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Jira.Activity.Api;
using Vuetrack.Connectors.Jira.Internal;
using Vuetrack.Framework.Extensions;

namespace Vuetrack.Connectors.Jira.Activity;

public static class JiraActivityMapper
{
    private const int MaxCommentLength = 500;

    public static ActivitySignal ToWorklogSignal(JiraIssueContext context, JiraWorklogResponse worklog, string siteUrl)
    {
        var started = worklog.Started!.Value;
        var ended = started.AddSeconds(worklog.TimeSpentSeconds);
        var extracted = AdfTextExtractor.Extract(worklog.Comment);
        var commentText = extracted.Truncate(MaxCommentLength);

        var detail = BaseDetail(context, siteUrl) with
        {
            ActorId = worklog.Author?.AccountId,
            CommentText = commentText,
            WorklogId = worklog.Id,
        };

        var externalId = $"{context.Key}:worklog:{worklog.Id}";

        return new ActivitySignal
        {
            ConnectorKey = ConnectorKey.Jira,
            ExternalId = externalId,
            DateStarted = started,
            DateEnded = ended,
            Kind = ActivityKind.Worklog,
            Detail = detail,
        };
    }

    public static ActivitySignal ToCommentSignal(JiraIssueContext context, JiraCommentResponse comment, string siteUrl)
    {
        var created = comment.Created!.Value;
        var extracted = AdfTextExtractor.Extract(comment.Body);
        var text = extracted.Truncate(MaxCommentLength);

        var detail = BaseDetail(context, siteUrl) with
        {
            ActorId = comment.Author?.AccountId,
            CommentText = text,
            CommentId = comment.Id,
        };

        var externalId = $"{context.Key}:comment:{comment.Id}";

        return new ActivitySignal
        {
            ConnectorKey = ConnectorKey.Jira,
            ExternalId = externalId,
            DateStarted = created,
            DateEnded = null,
            Kind = ActivityKind.Comment,
            Detail = detail,
        };
    }

    public static ActivitySignal ToChangeSignal(JiraIssueContext context, JiraChangelogResponse changelog, JiraChangelogItemResponse item, int itemIndex, string siteUrl)
    {
        var created = changelog.Created!.Value;
        var kind = Classify(item);
        var transition = new JiraFieldTransition(item.From, item.FromString, item.To, item.ToDisplay);

        var detail = BaseDetail(context, siteUrl) with
        {
            ActorId = changelog.Author?.AccountId,
            ChangelogId = changelog.Id,
            FieldId = item.FieldId,
            FieldName = item.Field,
            Transition = transition,
        };

        var externalId = $"{context.Key}:changelog:{changelog.Id}:{itemIndex}";

        return new ActivitySignal
        {
            ConnectorKey = ConnectorKey.Jira,
            ExternalId = externalId,
            DateStarted = created,
            DateEnded = null,
            Kind = kind,
            Detail = detail,
        };
    }

    // Classifies a changelog item into an activity kind the engine's evidence rules understand.
    public static ActivityKind Classify(JiraChangelogItemResponse item)
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

    private static JiraSignalDetail BaseDetail(JiraIssueContext context, string siteUrl)
    {
        var url = BrowseUrl(siteUrl, context.Key);

        return new JiraSignalDetail
        {
            IssueKey = context.Key,
            IssueId = context.Id,
            Summary = context.Summary,
            ProjectKey = context.ProjectKey,
            ProjectName = context.ProjectName,
            IssueType = context.IssueType,
            Status = context.Status,
            ParentKey = context.ParentKey,
            Labels = context.Labels,
            Components = context.Components,
            SourceUrl = url,
        };
    }

    private static string BrowseUrl(string siteUrl, string issueKey)
    {
        var trimmed = siteUrl.TrimEnd('/');
        return $"{trimmed}/browse/{issueKey}";
    }
}
