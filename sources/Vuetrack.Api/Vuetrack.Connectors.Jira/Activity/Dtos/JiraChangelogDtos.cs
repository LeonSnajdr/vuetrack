using System.Text.Json.Serialization;

namespace Vuetrack.Connectors.Jira.Activity.Dtos;

// Request/response for POST /rest/api/3/changelog/bulkfetch — fetches changelogs for many issues at
// once, filtered to a small set of field ids, paged by nextPageToken.

public sealed record JiraBulkChangelogRequestDto
{
    [JsonPropertyName("issueIdsOrKeys")]
    public required IReadOnlyList<string> IssueIdsOrKeys { get; init; }

    [JsonPropertyName("fieldIds")]
    public IReadOnlyList<string>? FieldIds { get; init; }

    [JsonPropertyName("maxResults")]
    public int? MaxResults { get; init; }

    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; init; }
}

public sealed record JiraChangelogItemDto
{
    [JsonPropertyName("field")]
    public string? Field { get; init; }

    [JsonPropertyName("fieldId")]
    public string? FieldId { get; init; }

    [JsonPropertyName("from")]
    public string? From { get; init; }

    [JsonPropertyName("fromString")]
    public string? FromString { get; init; }

    [JsonPropertyName("to")]
    public string? To { get; init; }

    [JsonPropertyName("toString")]
    public string? ToDisplay { get; init; }
}

public sealed record JiraChangelogDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("author")]
    public JiraUserDto? Author { get; init; }

    [JsonPropertyName("created")]
    public DateTimeOffset? Created { get; init; }

    [JsonPropertyName("items")]
    public IReadOnlyList<JiraChangelogItemDto>? Items { get; init; }
}

public sealed record JiraIssueChangeLogDto
{
    [JsonPropertyName("issueId")]
    public string? IssueId { get; init; }

    [JsonPropertyName("changeHistories")]
    public IReadOnlyList<JiraChangelogDto>? ChangeHistories { get; init; }
}

public sealed record JiraBulkChangelogResponseDto
{
    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; init; }

    [JsonPropertyName("issueChangeLogs")]
    public IReadOnlyList<JiraIssueChangeLogDto>? IssueChangeLogs { get; init; }
}
