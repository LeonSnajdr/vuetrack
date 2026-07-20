using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Jira.Activity.Api;

namespace Vuetrack.Connectors.Jira.Activity;

public static class JiraActivityMapper
{
    public static ActivitySignal ToWorklogSignal(JiraIssueContext context, JiraWorklogResponse worklog)
    {
        var started = worklog.Started!.Value;
        var ended = started.AddSeconds(worklog.TimeSpentSeconds);
        var detail = BaseDetail(context);

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

    public static ActivitySignal ToCommentSignal(JiraIssueContext context, JiraCommentResponse comment)
    {
        var created = comment.Created!.Value;
        var detail = BaseDetail(context);

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

    public static ActivitySignal ToChangeSignal(JiraIssueContext context, JiraChangelogResponse changelog, JiraChangelogItemResponse item, int itemIndex)
    {
        var created = changelog.Created!.Value;
        var kind = Classify(item);
        var transition = new JiraFieldTransition(item.FromString, item.ToDisplay);

        var detail = BaseDetail(context) with
        {
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

    private static ActivityKind Classify(JiraChangelogItemResponse item)
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

    private static JiraSignalDetail BaseDetail(JiraIssueContext context)
    {
        return new JiraSignalDetail
        {
            IssueKey = context.Key,
            IssueType = context.IssueType,
            Status = context.Status,
            ParentKey = context.ParentKey,
        };
    }
}
