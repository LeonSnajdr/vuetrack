using System.Text.Json.Serialization;

namespace Vuetrack.Connectors.Jira.Contracts;

public sealed record JiraAccessibleResourceResponse
{
    [JsonPropertyName("id")]
    public string CloudId { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
