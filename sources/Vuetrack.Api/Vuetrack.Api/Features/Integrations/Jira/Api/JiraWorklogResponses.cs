using System.Text.Json.Serialization;
using Vuetrack.Api.Features.Integrations.Jira.Api;

namespace Vuetrack.Api.Features.Integrations.Jira.Api;

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
