using System.Text.Json.Serialization;

namespace Vuetrack.Api.Features.Integrations.Jira.Api;

public sealed record JiraUserResponse
{
    [JsonPropertyName("accountId")]
    public string? AccountId { get; init; }
}
