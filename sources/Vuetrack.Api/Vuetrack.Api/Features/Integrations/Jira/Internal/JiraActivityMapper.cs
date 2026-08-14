using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Features.Integrations.Activity;
using Vuetrack.Api.Features.Integrations.Jira;
using Vuetrack.Api.Features.Integrations.Jira.Api;

namespace Vuetrack.Api.Features.Integrations.Jira.Internal;

public static class JiraActivityMapper
{
    public static ActivitySignal ToWorklogSignal(this JiraWorklogResponse worklog, JiraIssueContext context)
    {
        var started = worklog.Started!.Value;
        var ended = started.AddSeconds(worklog.TimeSpentSeconds);
        var detail = BaseDetail(context);

        var externalId = $"{context.Key}:worklog:{worklog.Id}";

        return new ActivitySignal
        {
            Key = IntegrationKey.Jira,
            ExternalId = externalId,
            DateStarted = started,
            DateEnded = ended,
            Kind = ActivityKind.Worklog,
            Detail = detail,
        };
    }

    public static ActivitySignal ToCommentSignal(this JiraCommentResponse comment, JiraIssueContext context)
    {
        var created = comment.Created!.Value;
        var detail = BaseDetail(context);

        var externalId = $"{context.Key}:comment:{comment.Id}";

        return new ActivitySignal
        {
            Key = IntegrationKey.Jira,
            ExternalId = externalId,
            DateStarted = created,
            DateEnded = null,
            Kind = ActivityKind.Comment,
            Detail = detail,
        };
    }

    public static ActivitySignal ToChangeSignal(this JiraChangelogResponse changelog, JiraIssueContext context, JiraChangelogItemResponse item, ActivityKind kind, int itemIndex)
    {
        var created = changelog.Created!.Value;
        var transition = new JiraFieldTransition(item.FromString, item.ToDisplay);

        var detail = BaseDetail(context) with
        {
            Transition = transition,
        };

        var externalId = $"{context.Key}:changelog:{changelog.Id}:{itemIndex}";

        return new ActivitySignal
        {
            Key = IntegrationKey.Jira,
            ExternalId = externalId,
            DateStarted = created,
            DateEnded = null,
            Kind = kind,
            Detail = detail,
        };
    }

    private static JiraSignalDetail BaseDetail(JiraIssueContext context)
    {
        return new JiraSignalDetail
        {
            IssueKey = context.Key,
            Title = context.Title,
            IssueType = context.IssueType,
            Status = context.Status,
            ParentKey = context.ParentKey,
            ParentTitle = context.ParentTitle,
        };
    }
}
