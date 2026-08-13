using System.Text.Json.Serialization;
using Vuetrack.Api.Features.Integrations.Github.Api;

namespace Vuetrack.Api.Features.Integrations.Github.Api;

public sealed record GithubCommitSearchResponse
{
    [JsonPropertyName("total_count")]
    public int TotalCount { get; init; }

    [JsonPropertyName("items")]
    public List<GithubCommitItemResponse>? Items { get; init; }
}

public sealed record GithubCommitItemResponse
{
    [JsonPropertyName("sha")]
    public string? Sha { get; init; }

    [JsonPropertyName("commit")]
    public GithubCommitResponse? Commit { get; init; }

    [JsonPropertyName("repository")]
    public GithubRepoResponse? Repository { get; init; }
}

public sealed record GithubCommitResponse
{
    [JsonPropertyName("author")]
    public GithubCommitAuthorResponse? Author { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

public sealed record GithubCommitAuthorResponse
{
    [JsonPropertyName("date")]
    public DateTime? Date { get; init; }
}

public sealed record GithubRepoResponse
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; init; }
}
