using Vuetrack.Connectors.Jira.Activity.Api;

namespace Vuetrack.Connectors.Jira.Activity;

// Connector-domain container: the per-issue context needed to build and correlate signals, mapped once
// from a search DTO so the event mappers never touch DTO field names again.
public sealed record JiraIssueContext
{
    public required string Key { get; init; }

    public string? Id { get; init; }

    public string? Summary { get; init; }

    public string? ProjectKey { get; init; }

    public string? ProjectName { get; init; }

    public string? IssueType { get; init; }

    public string? Status { get; init; }

    public string? ParentKey { get; init; }

    public IReadOnlyList<string> Labels { get; init; } = [];

    public IReadOnlyList<string> Components { get; init; } = [];

    public static JiraIssueContext FromResponse(JiraSearchIssueResponse response)
    {
        var fields = response.Fields;
        var components = MapComponents(fields?.Components);

        return new JiraIssueContext
        {
            Key = response.Key ?? string.Empty,
            Id = response.Id,
            Summary = fields?.Summary,
            ProjectKey = fields?.Project?.Key,
            ProjectName = fields?.Project?.Name,
            IssueType = fields?.IssueType?.Name,
            Status = fields?.Status?.Name,
            ParentKey = fields?.Parent?.Key,
            Labels = fields?.Labels ?? [],
            Components = components,
        };
    }

    private static IReadOnlyList<string> MapComponents(IReadOnlyList<JiraComponentResponse>? components)
    {
        if (components is null)
        {
            return [];
        }

        var names = components
            .Select(c => c.Name)
            .OfType<string>()
            .ToList();

        return names;
    }
}
