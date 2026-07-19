using System.Text.Json.Serialization;

namespace Vuetrack.Connectors.Github.OAuth;

public sealed record GithubUserResponse
{
    [JsonPropertyName("login")]
    public string Login { get; init; } = string.Empty;

    [JsonPropertyName("id")]
    public long Id { get; init; }
}
