namespace Vuetrack.Api.Features.Integrations.Github.Connection;

public sealed record GithubConnectionContext
{
    public required string UserId { get; init; }

    public required string AccessToken { get; init; }

    public required string Login { get; init; }
}

public static class GithubConnectionAttributes
{
    public const string Login = "login";
}
