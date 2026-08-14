using System.Text.Json.Serialization;

namespace Vuetrack.Api.Features.Integrations.Jira.Api;

public sealed record JiraSearchResponse
{
    [JsonPropertyName("issues")]
    public IReadOnlyList<JiraSearchIssueResponse>? Issues { get; init; }
}

public sealed record JiraSearchIssueResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("fields")]
    public JiraIssueFieldsResponse? Fields { get; init; }
}

public sealed record JiraIssueFieldsResponse
{
    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("issuetype")]
    public JiraNamedResponse? IssueType { get; init; }

    [JsonPropertyName("status")]
    public JiraNamedResponse? Status { get; init; }

    [JsonPropertyName("parent")]
    public JiraParentResponse? Parent { get; init; }
}

public sealed record JiraParentResponse
{
    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("fields")]
    public JiraParentFieldsResponse? Fields { get; init; }
}

public sealed record JiraParentFieldsResponse
{
    [JsonPropertyName("summary")]
    public string? Summary { get; init; }
}
