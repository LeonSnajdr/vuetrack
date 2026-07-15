using Vuetrack.Connectors.Jira.Activity.Dtos;

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

    public static JiraIssueContext FromDto(JiraSearchIssueDto dto)
    {
        var fields = dto.Fields;
        var components = MapComponents(fields?.Components);

        return new JiraIssueContext
        {
            Key = dto.Key ?? string.Empty,
            Id = dto.Id,
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

    private static IReadOnlyList<string> MapComponents(IReadOnlyList<JiraComponentDto>? components)
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
