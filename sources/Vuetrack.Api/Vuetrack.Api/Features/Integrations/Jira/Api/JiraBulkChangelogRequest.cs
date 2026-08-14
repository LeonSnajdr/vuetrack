using System.Text.Json.Serialization;

namespace Vuetrack.Api.Features.Integrations.Jira.Api;

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
