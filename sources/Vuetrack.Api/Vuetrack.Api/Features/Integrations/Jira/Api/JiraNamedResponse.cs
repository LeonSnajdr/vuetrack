using System.Text.Json.Serialization;

namespace Vuetrack.Api.Features.Integrations.Jira.Api;

public sealed record JiraNamedResponse
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
