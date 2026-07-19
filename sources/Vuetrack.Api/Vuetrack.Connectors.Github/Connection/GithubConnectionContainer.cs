namespace Vuetrack.Connectors.Github.Connection;

// Request-scoped, decrypted view of the connection published by the context factory.
public sealed record GithubConnectionContainer
{
    public required string UserId { get; init; }

    public required string AccessToken { get; init; }

    public required string Login { get; init; }
}
