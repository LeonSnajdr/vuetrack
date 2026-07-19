namespace Vuetrack.Connectors.Github;

public enum GithubApiErrorKind
{
    Auth,
    RateLimited,
    Transport,
}

public sealed class GithubApiException(GithubApiErrorKind kind, string message, TimeSpan? retryAfter = null) : Exception(message)
{
    public GithubApiErrorKind Kind { get; } = kind;

    public TimeSpan? RetryAfter { get; } = retryAfter;
}
