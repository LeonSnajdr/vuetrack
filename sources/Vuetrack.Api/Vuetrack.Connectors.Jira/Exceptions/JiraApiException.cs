namespace Vuetrack.Connectors.Jira.Exceptions;

public enum JiraApiErrorKind
{
    Auth,
    RateLimited,
    Transport,
}

public sealed class JiraApiException(JiraApiErrorKind kind, string message, TimeSpan? retryAfter = null)
    : Exception(message)
{
    public JiraApiErrorKind Kind { get; } = kind;

    public TimeSpan? RetryAfter { get; } = retryAfter;
}
