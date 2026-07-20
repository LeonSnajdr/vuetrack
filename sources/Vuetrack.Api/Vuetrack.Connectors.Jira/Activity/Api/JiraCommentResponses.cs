using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vuetrack.Connectors.Jira.Activity.Api;

public sealed record JiraCommentResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("author")]
    public JiraUserResponse? Author { get; init; }

    [JsonPropertyName("created")]
    public DateTime? Created { get; init; }

    [JsonPropertyName("updated")]
    public DateTime? Updated { get; init; }

    // Atlassian Document Format; flattened to text via AdfTextExtractor.
    [JsonPropertyName("body")]
    public JsonElement? Body { get; init; }
}

public sealed record JiraCommentPageResponse
{
    [JsonPropertyName("startAt")]
    public int StartAt { get; init; }

    [JsonPropertyName("maxResults")]
    public int MaxResults { get; init; }

    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("comments")]
    public IReadOnlyList<JiraCommentResponse>? Comments { get; init; }
}
