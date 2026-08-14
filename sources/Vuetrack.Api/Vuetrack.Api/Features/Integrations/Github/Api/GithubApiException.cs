namespace Vuetrack.Api.Features.Integrations.Github.Api;

public sealed class GithubApiException(GithubApiErrorKind kind, string message) : Exception(message)
{
    public GithubApiErrorKind Kind { get; } = kind;
}

public enum GithubApiErrorKind
{
    Auth,
    Transport,
}
