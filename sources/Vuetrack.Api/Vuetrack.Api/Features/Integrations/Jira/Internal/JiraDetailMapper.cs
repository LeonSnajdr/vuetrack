using Vuetrack.Api.Features.Details;
using Vuetrack.Api.Features.Details.Contracts;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Jira.Api;

namespace Vuetrack.Api.Features.Integrations.Jira.Internal;

public static class JiraDetailMapper
{
    public static IReadOnlyList<DetailField> ToDetailFields(this JiraSearchIssueResponse issue, string issueKey, string siteUrl)
    {
        var fields = new List<DetailField>();

        var type = issue.Fields?.IssueType?.Name;
        if (!string.IsNullOrWhiteSpace(type))
        {
            fields.Add(new ChipDetailField { Label = DetailFieldLabel.IssueType, Value = type });
        }

        var status = issue.Fields?.Status?.Name;
        if (!string.IsNullOrWhiteSpace(status))
        {
            fields.Add(new ChipDetailField { Label = DetailFieldLabel.Status, Value = status });
        }

        var summary = issue.Fields?.Summary;
        if (!string.IsNullOrWhiteSpace(summary))
        {
            fields.Add(new TextDetailField { Label = DetailFieldLabel.Title, Value = summary });
        }

        var url = BuildIssueUrl(siteUrl, issueKey);
        if (url is not null)
        {
            fields.Add(new LinkDetailField { Label = DetailFieldLabel.IssueLink, Text = issueKey, Url = url });
        }

        return fields;
    }

    private static string? BuildIssueUrl(string siteUrl, string issueKey)
    {
        if (string.IsNullOrWhiteSpace(siteUrl))
        {
            return null;
        }

        var trimmed = siteUrl.TrimEnd('/');
        var url = $"{trimmed}/browse/{Uri.EscapeDataString(issueKey)}";
        return url;
    }
}
