namespace Vuetrack.Api.Features.Integrations.Github.Api;

public enum GithubApiErrorKind
{
    Auth,
    Transport,
}

public sealed class GithubApiException(GithubApiErrorKind kind, string message) : Exception(message)
{
    public GithubApiErrorKind Kind { get; } = kind;
}
