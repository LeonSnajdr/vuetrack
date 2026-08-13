using Vuetrack.Api.Features.Integrations.Jira.Api;
using Vuetrack.Api.Features.Integrations.Jira.Internal;

namespace Vuetrack.Api.Features.Integrations.Jira.Internal;

public sealed record JiraIssueContext
{
    public required string Key { get; init; }

    public string? Id { get; init; }

    public string? Title { get; init; }

    public string? IssueType { get; init; }

    public string? Status { get; init; }

    public string? ParentKey { get; init; }

    public string? ParentTitle { get; init; }

    public static JiraIssueContext FromResponse(JiraSearchIssueResponse response)
    {
        var fields = response.Fields;
        var parent = fields?.Parent;

        return new JiraIssueContext
        {
            Key = response.Key ?? string.Empty,
            Id = response.Id,
            Title = fields?.Summary,
            IssueType = fields?.IssueType?.Name,
            Status = fields?.Status?.Name,
            ParentKey = parent?.Key,
            ParentTitle = parent?.Fields?.Summary,
        };
    }
}
