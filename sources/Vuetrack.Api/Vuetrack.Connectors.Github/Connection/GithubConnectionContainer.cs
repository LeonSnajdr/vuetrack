namespace Vuetrack.Connectors.Github.Connection;

public sealed record GithubConnectionContainer
{
    public required string UserId { get; init; }

    public required string AccessToken { get; init; }

    public required string Login { get; init; }
}
