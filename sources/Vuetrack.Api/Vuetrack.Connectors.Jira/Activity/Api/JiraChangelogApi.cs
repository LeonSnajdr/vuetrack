using System.Text.Json.Serialization;

namespace Vuetrack.Connectors.Jira.Activity.Api;

// Request/response for POST /rest/api/3/changelog/bulkfetch — fetches changelogs for many issues at
// once, filtered to a small set of field ids, paged by nextPageToken.

public sealed record JiraBulkChangelogRequest
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

public sealed record JiraChangelogItemResponse
{
    [JsonPropertyName("field")]
    public string? Field { get; init; }

    [JsonPropertyName("fieldId")]
    public string? FieldId { get; init; }

    [JsonPropertyName("fromString")]
    public string? FromString { get; init; }

    [JsonPropertyName("toString")]
    public string? ToDisplay { get; init; }
}

public sealed record JiraChangelogResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("author")]
    public JiraUserResponse? Author { get; init; }

    [JsonPropertyName("created")]
    public DateTime? Created { get; init; }

    [JsonPropertyName("items")]
    public IReadOnlyList<JiraChangelogItemResponse>? Items { get; init; }
}

public sealed record JiraIssueChangeLogResponse
{
    [JsonPropertyName("issueId")]
    public string? IssueId { get; init; }

    [JsonPropertyName("changeHistories")]
    public IReadOnlyList<JiraChangelogResponse>? ChangeHistories { get; init; }
}

public sealed record JiraBulkChangelogResponse
{
    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; init; }

    [JsonPropertyName("issueChangeLogs")]
    public IReadOnlyList<JiraIssueChangeLogResponse>? IssueChangeLogs { get; init; }
}
