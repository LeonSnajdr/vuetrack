using Vuetrack.Connectors.Abstractions;
using Vuetrack.Connectors.Jira.Containers;

namespace Vuetrack.Connectors.Jira.Mapping;

public static class JiraActivityMapper
{
    public static ActivitySignal ToActivitySignal(this JiraWorklogContainer worklog, JiraMapperContainer context) => new()
    {
        ConnectorKey = context.ConnectorKey,
        ExternalId = $"{worklog.IssueKey}:worklog:{worklog.WorklogId}",
        Title = $"{worklog.IssueKey} {worklog.IssueSummary}".TrimEnd(),
        Description = worklog.Comment,
        Start = worklog.Started,
        End = worklog.Started.AddSeconds(worklog.TimeSpentSeconds),
        Link = BrowseUrl(context.SiteUrl, worklog.IssueKey),
        Metadata = BuildMetadata(worklog.IssueKey, worklog.Project, worklog.IssueType, worklog.Status, ("worklogId", worklog.WorklogId)),
    };

    public static ActivitySignal ToActivitySignal(this JiraIssueActivityContainer issue, JiraMapperContainer context) => new()
    {
        ConnectorKey = context.ConnectorKey,
        ExternalId = $"{issue.IssueKey}:issue",
        Title = $"{issue.IssueKey} {issue.Summary}".TrimEnd(),
        Description = null,
        Start = issue.Updated,
        End = null,
        Link = BrowseUrl(context.SiteUrl, issue.IssueKey),
        Metadata = BuildMetadata(issue.IssueKey, issue.Project, issue.IssueType, issue.Status),
    };

    private static string BrowseUrl(string siteUrl, string issueKey) =>
        $"{siteUrl.TrimEnd('/')}/browse/{issueKey}";

    private static IReadOnlyDictionary<string, string> BuildMetadata(
        string issueKey,
        string? project,
        string? issueType,
        string? status,
        params (string Key, string Value)[] extra)
    {
        var metadata = new Dictionary<string, string> { ["issueKey"] = issueKey };

        if (!string.IsNullOrEmpty(project))
        {
            metadata["project"] = project;
        }

        if (!string.IsNullOrEmpty(issueType))
        {
            metadata["issueType"] = issueType;
        }

        if (!string.IsNullOrEmpty(status))
        {
            metadata["status"] = status;
        }

        foreach (var (key, value) in extra)
        {
            metadata[key] = value;
        }

        return metadata;
    }
}
