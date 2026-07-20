using System.Text.Json.Serialization;

namespace Vuetrack.Connectors.Jira.Activity.Api;

// Shared fragments used across several Jira REST responses. All Jira Cloud v3 responses are camelCase,
// so web-default deserialization matches; JsonPropertyName is added only where the CLR name would differ.

public sealed record JiraUserResponse
{
    [JsonPropertyName("accountId")]
    public string? AccountId { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }
}

public sealed record JiraNamedResponse
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

public sealed record JiraProjectResponse
{
    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

public sealed record JiraParentResponse
{
    [JsonPropertyName("key")]
    public string? Key { get; init; }
}

public sealed record JiraComponentResponse
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

public sealed record JiraIssueFieldsResponse
{
    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("issuetype")]
    public JiraNamedResponse? IssueType { get; init; }

    [JsonPropertyName("status")]
    public JiraNamedResponse? Status { get; init; }

    [JsonPropertyName("project")]
    public JiraProjectResponse? Project { get; init; }

    [JsonPropertyName("parent")]
    public JiraParentResponse? Parent { get; init; }

    [JsonPropertyName("labels")]
    public IReadOnlyList<string>? Labels { get; init; }

    [JsonPropertyName("components")]
    public IReadOnlyList<JiraComponentResponse>? Components { get; init; }

    [JsonPropertyName("updated")]
    public DateTime? Updated { get; init; }
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

public sealed record JiraSearchRequest
{
    [JsonPropertyName("jql")]
    public required string Jql { get; init; }

    [JsonPropertyName("fields")]
    public required IReadOnlyList<string> Fields { get; init; }

    [JsonPropertyName("maxResults")]
    public required int MaxResults { get; init; }
}

public sealed record JiraSearchResponse
{
    [JsonPropertyName("issues")]
    public IReadOnlyList<JiraSearchIssueResponse>? Issues { get; init; }
}
