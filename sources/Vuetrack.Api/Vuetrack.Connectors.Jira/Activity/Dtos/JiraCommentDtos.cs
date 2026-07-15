using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vuetrack.Connectors.Jira.Activity.Dtos;

public sealed record JiraCommentDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("author")]
    public JiraUserDto? Author { get; init; }

    [JsonPropertyName("created")]
    public DateTimeOffset? Created { get; init; }

    [JsonPropertyName("updated")]
    public DateTimeOffset? Updated { get; init; }

    // Atlassian Document Format; flattened to text via AdfTextExtractor.
    [JsonPropertyName("body")]
    public JsonElement? Body { get; init; }
}

public sealed record JiraCommentResponseDto
{
    [JsonPropertyName("startAt")]
    public int StartAt { get; init; }

    [JsonPropertyName("maxResults")]
    public int MaxResults { get; init; }

    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("comments")]
    public IReadOnlyList<JiraCommentDto>? Comments { get; init; }
}
