namespace Vuetrack.Connectors.Jira;

public enum JiraApiErrorKind
{
    Auth,
    RateLimited,
    Transport,
}

public sealed class JiraApiException(JiraApiErrorKind kind, string message, TimeSpan? retryAfter = null) : Exception(message)
{
    public JiraApiErrorKind Kind { get; } = kind;

    public TimeSpan? RetryAfter { get; } = retryAfter;
}
