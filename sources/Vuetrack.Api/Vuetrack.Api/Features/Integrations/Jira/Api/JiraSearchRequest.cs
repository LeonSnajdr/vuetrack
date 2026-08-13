using System.Text.Json.Serialization;

namespace Vuetrack.Api.Features.Integrations.Jira.Api;

public sealed record JiraSearchRequest
{
    [JsonPropertyName("jql")]
    public required string Jql { get; init; }

    [JsonPropertyName("fields")]
    public required IReadOnlyList<string> Fields { get; init; }

    [JsonPropertyName("maxResults")]
    public required int MaxResults { get; init; }
}
