using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vuetrack.Connectors.Jira.Activity.Api;

public sealed record JiraWorklogResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("author")]
    public JiraUserResponse? Author { get; init; }

    [JsonPropertyName("started")]
    public DateTime? Started { get; init; }

    [JsonPropertyName("timeSpentSeconds")]
    public long TimeSpentSeconds { get; init; }

    // Atlassian Document Format; kept as a raw element and flattened to text via AdfTextExtractor.
    [JsonPropertyName("comment")]
    public JsonElement? Comment { get; init; }
}

public sealed record JiraWorklogPageResponse
{
    [JsonPropertyName("startAt")]
    public int StartAt { get; init; }

    [JsonPropertyName("maxResults")]
    public int MaxResults { get; init; }

    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("worklogs")]
    public IReadOnlyList<JiraWorklogResponse>? Worklogs { get; init; }
}
