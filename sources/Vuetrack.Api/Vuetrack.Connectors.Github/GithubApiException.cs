namespace Vuetrack.Connectors.Github;

public enum GithubApiErrorKind
{
    Auth,
    Transport,
}

public sealed class GithubApiException(GithubApiErrorKind kind, string message) : Exception(message)
{
    public GithubApiErrorKind Kind { get; } = kind;
}
