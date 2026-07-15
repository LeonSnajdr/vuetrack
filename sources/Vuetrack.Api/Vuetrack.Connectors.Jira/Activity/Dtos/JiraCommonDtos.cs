using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vuetrack.Connectors.Jira.Activity.Dtos;

// Shared fragments used across several Jira REST responses. All Jira Cloud v3 responses are camelCase,
// so web-default deserialization matches; JsonPropertyName is added only where the CLR name would differ.

public sealed record JiraUserDto
{
    [JsonPropertyName("accountId")]
    public string? AccountId { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }
}

public sealed record JiraNamedDto
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

public sealed record JiraProjectDto
{
    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

public sealed record JiraParentDto
{
    [JsonPropertyName("key")]
    public string? Key { get; init; }
}

public sealed record JiraComponentDto
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

public sealed record JiraIssueFieldsDto
{
    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("issuetype")]
    public JiraNamedDto? IssueType { get; init; }

    [JsonPropertyName("status")]
    public JiraNamedDto? Status { get; init; }

    [JsonPropertyName("project")]
    public JiraProjectDto? Project { get; init; }

    [JsonPropertyName("parent")]
    public JiraParentDto? Parent { get; init; }

    [JsonPropertyName("labels")]
    public IReadOnlyList<string>? Labels { get; init; }

    [JsonPropertyName("components")]
    public IReadOnlyList<JiraComponentDto>? Components { get; init; }

    [JsonPropertyName("updated")]
    public DateTimeOffset? Updated { get; init; }
}

public sealed record JiraSearchIssueDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("fields")]
    public JiraIssueFieldsDto? Fields { get; init; }
}

public sealed record JiraSearchResponseDto
{
    [JsonPropertyName("issues")]
    public IReadOnlyList<JiraSearchIssueDto>? Issues { get; init; }

    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; init; }

    [JsonPropertyName("isLast")]
    public bool? IsLast { get; init; }
}
