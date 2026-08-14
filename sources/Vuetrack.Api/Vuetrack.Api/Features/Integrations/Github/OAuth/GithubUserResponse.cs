using System.Text.Json.Serialization;
using Vuetrack.Api.Features.Integrations.Github.Api;

namespace Vuetrack.Api.Features.Integrations.Github.OAuth;

public sealed record GithubUserResponse
{
    [JsonPropertyName("login")]
    public string Login { get; init; } = string.Empty;

    [JsonPropertyName("id")]
    public long Id { get; init; }
}
